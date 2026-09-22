using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BankFlow.Api.Observability;

public static class BankFlowTelemetry
{
    public const string ServiceName = "BankFlow.Api";
    public const string ActivitySourceName = "BankFlow.Api";
    public const string MeterName = "BankFlow.Api";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    public static readonly Meter Meter =
        new(MeterName);

    public static readonly Counter<long> TransactionsAccepted =
        Meter.CreateCounter<long>(
            "bankflow.transactions.accepted",
            description: "Quantidade de transações aceitas pela API.");

    public static readonly Counter<long> OutboxPublished =
        Meter.CreateCounter<long>(
            "bankflow.outbox.published",
            description: "Quantidade de eventos publicados pela Outbox.");

    public static readonly Counter<long> OutboxFailures =
        Meter.CreateCounter<long>(
            "bankflow.outbox.failures",
            description: "Quantidade de falhas na publicação da Outbox.");
}
