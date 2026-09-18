using BankFlow.Contracts.Enums;

namespace BankFlow.Api.Entities;

public sealed class TransactionEntity
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CorrelationId { get; set; }

    public string Status { get; set; } = "Pending";
}
