using BankFlow.Contracts.Enums;
using BankFlow.Contracts.Events;
using BankFlow.Worker.Data;
using BankFlow.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BankFlow.Worker.Tests.Consumers;

public sealed class TransactionCreatedConsumerTests
{
    [Fact]
    public async Task ProcessAsync_ShouldPersistNewMessage()
    {
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext);

        var messageId = Guid.NewGuid();
        var message = CreateTransactionCreated();

        var result = await processor.ProcessAsync(
            messageId,
            message,
            CancellationToken.None);

        Assert.True(result);

        var stored = await dbContext.InboxMessages.SingleAsync();

        Assert.Equal(messageId, stored.MessageId);
        Assert.Equal(message.TransactionId, stored.TransactionId);
        Assert.Equal(message.CorrelationId, stored.CorrelationId);
        Assert.Equal(
            typeof(TransactionCreated).FullName,
            stored.MessageType);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreDuplicateMessageId()
    {
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext);

        var messageId = Guid.NewGuid();
        var message = CreateTransactionCreated();

        var first = await processor.ProcessAsync(
            messageId,
            message,
            CancellationToken.None);

        var second = await processor.ProcessAsync(
            messageId,
            message,
            CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(
            1,
            await dbContext.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_ShouldProcessDifferentMessages()
    {
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext);

        var first = await processor.ProcessAsync(
            Guid.NewGuid(),
            CreateTransactionCreated(),
            CancellationToken.None);

        var second = await processor.ProcessAsync(
            Guid.NewGuid(),
            CreateTransactionCreated(),
            CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(
            2,
            await dbContext.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_ShouldUseMessageIdForIdempotency()
    {
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext);

        var transaction = CreateTransactionCreated();

        var first = await processor.ProcessAsync(
            Guid.NewGuid(),
            transaction,
            CancellationToken.None);

        var second = await processor.ProcessAsync(
            Guid.NewGuid(),
            transaction,
            CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
        Assert.Equal(
            2,
            await dbContext.InboxMessages.CountAsync());
    }

    private static TransactionEventProcessor CreateProcessor(
        WorkerDbContext dbContext)
    {
        return new TransactionEventProcessor(
            dbContext,
            NullLogger<TransactionEventProcessor>.Instance);
    }

    private static WorkerDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<WorkerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new WorkerDbContext(options);
    }

    private static TransactionCreated CreateTransactionCreated()
    {
        return new TransactionCreated(
            Guid.NewGuid(),
            150.50m,
            TransactionType.Credit,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());
    }
}
