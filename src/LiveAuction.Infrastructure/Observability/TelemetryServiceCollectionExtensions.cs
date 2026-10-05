using LiveAuction.Application.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LiveAuction.Infrastructure.Observability;

public static class TelemetryServiceCollectionExtensions
{
    private const string OtlpEndpointSetting = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static OpenTelemetryBuilder AddAuctionTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing.AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddMeter(AuctionMetrics.MeterName)
                .AddRuntimeInstrumentation())
            .WithLogging();

        if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointSetting]))
        {
            telemetry.UseOtlpExporter();
        }

        return telemetry;
    }
}
