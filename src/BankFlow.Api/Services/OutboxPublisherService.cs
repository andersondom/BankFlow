using System.Text.Json;
using BankFlow.Api.Data;
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
            try
            {
                if (message.Type != typeof(TransactionCreated).FullName)
                {
                    throw new InvalidOperationException(
                        $"Tipo de evento não suportado: {message.Type}");
                }

                var transactionCreated =
                    JsonSerializer.Deserialize<TransactionCreated>(
                        message.Payload)
                    ?? throw new InvalidOperationException(
                        "Não foi possível desserializar TransactionCreated.");

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

                logger.LogInformation(
                    "OutboxMessage {OutboxMessageId} publicada. TransactionId: {TransactionId}, CorrelationId: {CorrelationId}",
                    message.Id,
                    transactionCreated.TransactionId,
                    message.CorrelationId);
            }
            catch (Exception exception)
            {
                message.RetryCount++;
                message.Error = exception.Message;

                logger.LogError(
                    exception,
                    "Falha ao publicar OutboxMessage {OutboxMessageId}. Tentativa: {RetryCount}",
                    message.Id,
                    message.RetryCount);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
