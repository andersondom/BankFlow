namespace BankFlow.Api.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public Guid CorrelationId { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? Error { get; set; }

    public int RetryCount { get; set; }
}
