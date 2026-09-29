using System.Diagnostics;
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

        context.Headers.TryGetHeader(
            "traceparent",
            out var traceParentHeader);

        context.Headers.TryGetHeader(
            "tracestate",
            out var traceStateHeader);

        var traceParent = traceParentHeader?.ToString();
        var traceState = traceStateHeader?.ToString();

        ActivityContext? parentContext = null;

        if (!string.IsNullOrWhiteSpace(traceParent)
            && ActivityContext.TryParse(
                traceParent,
                traceState,
                out var parsedContext))
        {
            parentContext = parsedContext;
        }

        await processor.ProcessAsync(
            context.MessageId.Value,
            context.Message,
            context.CancellationToken,
            parentContext);
    }
}
