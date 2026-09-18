using System.Text.Json;
using BankFlow.Api.Data;
using BankFlow.Api.Entities;
using BankFlow.Api.Models;
using BankFlow.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace BankFlow.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/transactions")
            .WithTags("Transactions");

        group.MapPost("/", CreateTransaction)
            .WithName("CreateTransaction")
            .WithSummary(
                "Persiste uma transação e registra seu evento na Transactional Outbox")
            .Produces<CreateTransactionResponse>(
                StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetTransaction)
            .WithName("GetTransaction")
            .WithSummary("Consulta uma transação pelo identificador")
            .Produces<TransactionEntity>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        BankFlowDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var transactionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var correlationId = TryGetCorrelationId(
            httpContext.Request.Headers[
                "X-Correlation-Id"].FirstOrDefault());

        var transaction = new TransactionEntity
        {
            Id = transactionId,
            Amount = request.Amount,
            Type = request.Type,
            CreatedAt = now,
            CorrelationId = correlationId,
            Status = "Pending"
        };

        var transactionCreated = new TransactionCreated(
            transaction.Id,
            transaction.Amount,
            transaction.Type,
            transaction.CreatedAt,
            transaction.CorrelationId);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredAt = now,
            Type = typeof(TransactionCreated).FullName!,
            Payload = JsonSerializer.Serialize(transactionCreated),
            CorrelationId = correlationId
        };

        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.Transactions.Add(transaction);
        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(cancellationToken);

        await databaseTransaction.CommitAsync(cancellationToken);

        var response = new CreateTransactionResponse(
            transactionId,
            correlationId,
            "Accepted");

        return Results.Accepted(
            $"/transactions/{transactionId}",
            response);
    }

    private static async Task<IResult> GetTransaction(
        Guid id,
        BankFlowDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id,
                cancellationToken);

        return transaction is null
            ? Results.NotFound()
            : Results.Ok(transaction);
    }

    private static Dictionary<string, string[]> Validate(
        CreateTransactionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Amount <= 0)
        {
            errors["amount"] =
            [
                "O valor da transação deve ser maior que zero."
            ];
        }

        if (!Enum.IsDefined(request.Type))
        {
            errors["type"] =
            [
                "O tipo da transação informado é inválido."
            ];
        }

        return errors;
    }

    private static Guid TryGetCorrelationId(string? value)
    {
        return Guid.TryParse(value, out var correlationId)
            ? correlationId
            : Guid.NewGuid();
    }
}
