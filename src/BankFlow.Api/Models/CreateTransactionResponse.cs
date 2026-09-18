namespace BankFlow.Api.Models;

public sealed record CreateTransactionResponse(
    Guid TransactionId,
    Guid CorrelationId,
    string Status);
