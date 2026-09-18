using BankFlow.Contracts.Events;
using BankFlow.Worker.Data;
using BankFlow.Worker.Entities;
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
        var alreadyProcessed = await dbContext.InboxMessages
            .AsNoTracking()
            .AnyAsync(
                message => message.MessageId == messageId,
                cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation(
                "Mensagem duplicada ignorada. MessageId: {MessageId}, TransactionId: {TransactionId}",
                messageId,
                transaction.TransactionId);

            return false;
        }

        var now = DateTimeOffset.UtcNow;

        var inboxMessage = new InboxMessage
        {
            MessageId = messageId,
            TransactionId = transaction.TransactionId,
            CorrelationId = transaction.CorrelationId,
            MessageType = typeof(TransactionCreated).FullName!,
            ReceivedAt = now,
            ProcessedAt = now
        };

        dbContext.InboxMessages.Add(inboxMessage);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(inboxMessage).State = EntityState.Detached;

            var duplicateConfirmed = await dbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(
                    message => message.MessageId == messageId,
                    cancellationToken);

            if (!duplicateConfirmed)
            {
                throw;
            }

            logger.LogInformation(
                "Mensagem duplicada detectada durante a persistência. MessageId: {MessageId}",
                messageId);

            return false;
        }

        logger.LogInformation(
            "TransactionCreated processado. MessageId: {MessageId}, TransactionId: {TransactionId}, Amount: {Amount}, Type: {Type}, CorrelationId: {CorrelationId}",
            messageId,
            transaction.TransactionId,
            transaction.Amount,
            transaction.Type,
            transaction.CorrelationId);

        return true;
    }
}
