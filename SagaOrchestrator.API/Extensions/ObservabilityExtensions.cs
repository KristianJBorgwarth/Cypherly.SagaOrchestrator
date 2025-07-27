using MassTransit.Logging;
using MassTransit.Monitoring;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SagaOrchestrator.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: "cypherly.chatserver.svc",
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName))

            .WithTracing(b => b
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddOtlpExporter())

            .WithMetrics(b => b
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(InstrumentationOptions.MeterName)
                .AddPrometheusExporter());
    }
}