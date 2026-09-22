using System.Diagnostics;
using System.Text.Json;
using BankFlow.Api.Data;
using BankFlow.Api.Observability;
using BankFlow.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace BankFlow.Api.Services;

public sealed class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherService> logger)
    : BackgroundService
{
    private const int BatchSize = 20;

    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Publisher iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Erro durante o processamento da Outbox.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessages(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<BankFlowDbContext>();

        var publishEndpoint =
            scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            ActivityContext parentContext = default;

            var hasParentContext =
                !string.IsNullOrWhiteSpace(message.TraceParent)
                && ActivityContext.TryParse(
                    message.TraceParent,
                    message.TraceState,
                    out parentContext);

            using var activity =
                hasParentContext
                    ? BankFlowTelemetry.ActivitySource.StartActivity(
                        "bankflow.outbox.publish",
                        ActivityKind.Producer,
                        parentContext)
                    : BankFlowTelemetry.ActivitySource.StartActivity(
                        "bankflow.outbox.publish",
                        ActivityKind.Producer);

            activity?.SetTag(
                "bankflow.outbox.id",
                message.Id);

            activity?.SetTag(
                "bankflow.correlation_id",
                message.CorrelationId);

            try
            {
                if (message.Type !=
                    typeof(TransactionCreated).FullName)
                {
                    throw new InvalidOperationException(
                        $"Tipo de evento não suportado: {message.Type}");
                }

                var transactionCreated =
                    JsonSerializer.Deserialize<TransactionCreated>(
                        message.Payload)
                    ?? throw new InvalidOperationException(
                        "Não foi possível desserializar TransactionCreated.");

                activity?.SetTag(
                    "bankflow.transaction.id",
                    transactionCreated.TransactionId);

                await publishEndpoint.Publish(
                    transactionCreated,
                    context =>
                    {
                        context.CorrelationId =
                            message.CorrelationId;
                    },
                    cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Error = null;

                activity?.SetStatus(ActivityStatusCode.Ok);

                BankFlowTelemetry.OutboxPublished.Add(1);

                logger.LogInformation(
                    "OutboxMessage {OutboxMessageId} publicada. TransactionId: {TransactionId}, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                    message.Id,
                    transactionCreated.TransactionId,
                    message.CorrelationId,
                    activity?.TraceId);
            }
            catch (Exception exception)
            {
                message.RetryCount++;
                message.Error = exception.Message;

                activity?.SetStatus(
                    ActivityStatusCode.Error,
                    exception.Message);

                BankFlowTelemetry.OutboxFailures.Add(1);

                logger.LogError(
                    exception,
                    "Falha ao publicar OutboxMessage {OutboxMessageId}. Tentativa: {RetryCount}, TraceId: {TraceId}",
                    message.Id,
                    message.RetryCount,
                    activity?.TraceId);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
