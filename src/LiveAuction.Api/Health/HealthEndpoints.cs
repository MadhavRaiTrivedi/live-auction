using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LiveAuction.Api.Health;

internal static class HealthEndpoints
{
    public const string ReadinessTag = "ready";

    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
        });
    }
}
