using BankFlow.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankFlow.Api.Data;

public sealed class BankFlowDbContext(
    DbContextOptions<BankFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<TransactionEntity> Transactions =>
        Set<TransactionEntity>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.ToTable("Transactions");

            entity.HasKey(transaction => transaction.Id);

            entity.Property(transaction => transaction.Amount)
                .HasPrecision(18, 2);

            entity.Property(transaction => transaction.Type)
                .HasConversion<int>();

            entity.Property(transaction => transaction.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(transaction => transaction.CorrelationId);
            entity.HasIndex(transaction => transaction.CreatedAt);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Type)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(message => message.Payload)
                .IsRequired();

            entity.Property(message => message.Error)
                .HasMaxLength(2000);

            entity.HasIndex(message => new
            {
                message.ProcessedAt,
                message.OccurredAt
            });

            entity.HasIndex(message => message.CorrelationId);
        });
    }
}
