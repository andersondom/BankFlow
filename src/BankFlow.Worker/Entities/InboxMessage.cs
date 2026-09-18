namespace BankFlow.Worker.Entities;

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }

    public Guid TransactionId { get; set; }

    public Guid CorrelationId { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
