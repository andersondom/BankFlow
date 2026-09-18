using BankFlow.Api.Models;
using BankFlow.Contracts.Events;
using MassTransit;

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
            .WithSummary("Cria uma transação e publica o evento TransactionCreated")
            .Produces<CreateTransactionResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        IPublishEndpoint publishEndpoint,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var transactionId = Guid.NewGuid();

        var correlationId = TryGetCorrelationId(
            httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault());

        var transactionCreated = new TransactionCreated(
            transactionId,
            request.Amount,
            request.Type,
            DateTimeOffset.UtcNow,
            correlationId);

        await publishEndpoint.Publish(
            transactionCreated,
            context =>
            {
                context.CorrelationId = correlationId;
            },
            cancellationToken);

        var response = new CreateTransactionResponse(
            transactionId,
            correlationId,
            "Published");

        return Results.Accepted(
            $"/transactions/{transactionId}",
            response);
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
