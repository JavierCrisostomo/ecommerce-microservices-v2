using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ECommerce.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddOpenTelemetryTracing(
        this WebApplicationBuilder builder,
        string serviceName,
        bool includeEfCore = true,
        bool includeMassTransit = true)
    {
        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();

                if (includeEfCore)
                    tracing.AddEntityFrameworkCoreInstrumentation();

                if (includeMassTransit)
                    tracing.AddSource("MassTransit");

                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            });

        return builder;
    }
}
