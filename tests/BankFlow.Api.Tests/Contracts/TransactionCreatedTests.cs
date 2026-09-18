using BankFlow.Contracts.Enums;
using BankFlow.Contracts.Events;

namespace BankFlow.Api.Tests.Contracts;

public class TransactionCreatedTests
{
    [Fact]
    public void TransactionCreated_DevePreservarOsDadosInformados()
    {
        var transactionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var transaction = new TransactionCreated(
            transactionId,
            150.50m,
            TransactionType.Credit,
            createdAt,
            correlationId);

        Assert.Equal(transactionId, transaction.TransactionId);
        Assert.Equal(150.50m, transaction.Amount);
        Assert.Equal(TransactionType.Credit, transaction.Type);
        Assert.Equal(createdAt, transaction.CreatedAt);
        Assert.Equal(correlationId, transaction.CorrelationId);
    }
}
