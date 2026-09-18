using BankFlow.Contracts.Enums;
using BankFlow.Contracts.Events;
using BankFlow.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankFlow.Worker.Tests.Consumers;

public class TransactionCreatedConsumerTests
{
    [Fact]
    public async Task Consumer_DeveProcessarTransactionCreated()
    {
        // Arrange
        var consumer = new TransactionCreatedConsumer(
            NullLogger<TransactionCreatedConsumer>.Instance);

        var message = new TransactionCreated(
            Guid.NewGuid(),
            250.00m,
            TransactionType.Transfer,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var context = Mock.Of<ConsumeContext<TransactionCreated>>(
            x => x.Message == message);

        // Act
        await consumer.Consume(context);

        // Assert
        Assert.Equal(250.00m, message.Amount);
        Assert.Equal(TransactionType.Transfer, message.Type);
    }
}
