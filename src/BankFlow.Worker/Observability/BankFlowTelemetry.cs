using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BankFlow.Worker.Observability;

public static class BankFlowTelemetry
{
    public const string ServiceName = "BankFlow.Worker";
    public const string ActivitySourceName = "BankFlow.Worker";
    public const string MeterName = "BankFlow.Worker";

    public static readonly ActivitySource ActivitySource =
        new(ActivitySourceName);

    public static readonly Meter Meter =
        new(MeterName);

    public static readonly Counter<long> MessagesProcessed =
        Meter.CreateCounter<long>(
            "bankflow.messages.processed",
            description: "Quantidade de mensagens processadas.");

    public static readonly Counter<long> DuplicateMessages =
        Meter.CreateCounter<long>(
            "bankflow.messages.duplicates",
            description: "Quantidade de mensagens duplicadas ignoradas.");
}
