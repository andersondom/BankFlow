using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BankFlow.Api.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddBankFlowObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var endpoint =
            configuration["OpenTelemetry:OtlpEndpoint"]
            ?? "http://localhost:4317";

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(
                    serviceName: BankFlowTelemetry.ServiceName,
                    serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(BankFlowTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(endpoint);
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(BankFlowTelemetry.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        return services;
    }
}
