using BankFlow.Worker.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankFlow.Worker.Data;

public sealed class WorkerDbContext(
    DbContextOptions<WorkerDbContext> options)
    : DbContext(options)
{
    public DbSet<InboxMessage> InboxMessages =>
        Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");

            entity.HasKey(message => message.MessageId);

            entity.Property(message => message.MessageType)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(message => message.TransactionId);
            entity.HasIndex(message => message.CorrelationId);
            entity.HasIndex(message => message.ProcessedAt);
        });
    }
}
