using BankFlow.Contracts.Enums;

namespace BankFlow.Contracts.Events;

public sealed record TransactionCreated(
    Guid TransactionId,
    decimal Amount,
    TransactionType Type,
    DateTimeOffset CreatedAt,
    Guid CorrelationId);
