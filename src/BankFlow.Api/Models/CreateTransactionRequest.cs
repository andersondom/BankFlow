using BankFlow.Contracts.Enums;

namespace BankFlow.Api.Models;

public sealed record CreateTransactionRequest(
    decimal Amount,
    TransactionType Type);
