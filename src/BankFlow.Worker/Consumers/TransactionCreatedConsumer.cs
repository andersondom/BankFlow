using BankFlow.Contracts.Events;
using MassTransit;

namespace BankFlow.Worker.Consumers;

public sealed class TransactionCreatedConsumer(
    ILogger<TransactionCreatedConsumer> logger)
    : IConsumer<TransactionCreated>
{
    public Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var transaction = context.Message;

        logger.LogInformation(
            "TransactionCreated recebido. TransactionId: {TransactionId}, " +
            "Amount: {Amount}, Type: {Type}, CorrelationId: {CorrelationId}, " +
            "CreatedAt: {CreatedAt}",
            transaction.TransactionId,
            transaction.Amount,
            transaction.Type,
            transaction.CorrelationId,
            transaction.CreatedAt);

        return Task.CompletedTask;
    }
}
