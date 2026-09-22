using System.Diagnostics;
using BankFlow.Contracts.Events;
using BankFlow.Worker.Data;
using BankFlow.Worker.Entities;
using BankFlow.Worker.Observability;
using Microsoft.EntityFrameworkCore;

namespace BankFlow.Worker.Services;

public sealed class TransactionEventProcessor(
    WorkerDbContext dbContext,
    ILogger<TransactionEventProcessor> logger)
{
    public async Task<bool> ProcessAsync(
        Guid messageId,
        TransactionCreated transaction,
        CancellationToken cancellationToken)
    {
        using var activity =
            BankFlowTelemetry.ActivitySource.StartActivity(
                "bankflow.transaction.process",
                ActivityKind.Consumer);

        activity?.SetTag(
            "messaging.message.id",
            messageId);

        activity?.SetTag(
            "bankflow.transaction.id",
            transaction.TransactionId);

        activity?.SetTag(
            "bankflow.correlation_id",
            transaction.CorrelationId);

        var alreadyProcessed =
            await dbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(
                    message =>
                        message.MessageId == messageId,
                    cancellationToken);

        if (alreadyProcessed)
        {
            activity?.SetTag(
                "bankflow.duplicate",
                true);

            activity?.SetStatus(
                ActivityStatusCode.Ok);

            BankFlowTelemetry.DuplicateMessages.Add(1);

            logger.LogInformation(
                "Mensagem duplicada ignorada. MessageId: {MessageId}, TransactionId: {TransactionId}, TraceId: {TraceId}",
                messageId,
                transaction.TransactionId,
                activity?.TraceId);

            return false;
        }

        var now = DateTimeOffset.UtcNow;

        var inboxMessage = new InboxMessage
        {
            MessageId = messageId,
            TransactionId = transaction.TransactionId,
            CorrelationId = transaction.CorrelationId,
            MessageType =
                typeof(TransactionCreated).FullName!,
            ReceivedAt = now,
            ProcessedAt = now
        };

        dbContext.InboxMessages.Add(inboxMessage);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(inboxMessage).State =
                EntityState.Detached;

            var duplicateConfirmed =
                await dbContext.InboxMessages
                    .AsNoTracking()
                    .AnyAsync(
                        message =>
                            message.MessageId == messageId,
                        cancellationToken);

            if (!duplicateConfirmed)
            {
                activity?.SetStatus(
                    ActivityStatusCode.Error);

                throw;
            }

            activity?.SetTag(
                "bankflow.duplicate",
                true);

            activity?.SetStatus(
                ActivityStatusCode.Ok);

            BankFlowTelemetry.DuplicateMessages.Add(1);

            logger.LogInformation(
                "Mensagem duplicada detectada durante a persistência. MessageId: {MessageId}, TraceId: {TraceId}",
                messageId,
                activity?.TraceId);

            return false;
        }

        activity?.SetStatus(
            ActivityStatusCode.Ok);

        BankFlowTelemetry.MessagesProcessed.Add(1);

        logger.LogInformation(
            "TransactionCreated processado. MessageId: {MessageId}, TransactionId: {TransactionId}, Amount: {Amount}, Type: {Type}, CorrelationId: {CorrelationId}, TraceId: {TraceId}",
            messageId,
            transaction.TransactionId,
            transaction.Amount,
            transaction.Type,
            transaction.CorrelationId,
            activity?.TraceId);

        return true;
    }
}
