using BankFlow.Contracts.Events;
using BankFlow.Worker.Services;
using MassTransit;

namespace BankFlow.Worker.Consumers;

public sealed class TransactionCreatedConsumer(
    TransactionEventProcessor processor,
    ILogger<TransactionCreatedConsumer> logger)
    : IConsumer<TransactionCreated>
{
    public async Task Consume(
        ConsumeContext<TransactionCreated> context)
    {
        if (!context.MessageId.HasValue)
        {
            logger.LogWarning(
                "TransactionCreated recebido sem MessageId. TransactionId: {TransactionId}",
                context.Message.TransactionId);

            throw new InvalidOperationException(
                "A mensagem recebida não possui MessageId.");
        }

        await processor.ProcessAsync(
            context.MessageId.Value,
            context.Message,
            context.CancellationToken);
    }
}
