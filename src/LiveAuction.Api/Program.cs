using System.Text.Json.Serialization;
using LiveAuction.Api.Auctions;
using LiveAuction.Api.Bidding;
using LiveAuction.Api.Health;
using LiveAuction.Api.Http;
using LiveAuction.Api.Lifecycle;
using LiveAuction.Api.MyActivity;
using LiveAuction.Api.OpenApi;
using LiveAuction.Api.Realtime;
using LiveAuction.Api.Security;
using LiveAuction.Application;
using LiveAuction.Application.Security;
using LiveAuction.Infrastructure;
using LiveAuction.Infrastructure.Observability;
using LiveAuction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

const string ServiceName = "live-auction-api";
const string ApplyMigrationsSetting = "Database:ApplyMigrationsOnStartup";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuctionTelemetry(builder.Configuration, ServiceName)
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddBidRateLimiting();
builder.Services.AddRealtime(builder.Configuration);

var lifecycleSection = builder.Configuration.GetSection(LifecycleOptions.SectionName);
builder.Services.AddOptions<LifecycleOptions>().Bind(lifecycleSection);
if (lifecycleSection.Get<LifecycleOptions>()?.IsEnabled ?? true)
{
    builder.Services.AddHostedService<AuctionLifecycleJob>();
}

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AuctionExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddBearerSecurity());
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: [HealthEndpoints.ReadinessTag]);

var app = builder.Build();

if (app.Configuration.GetValue<bool>(ApplyMigrationsSetting))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AuctionDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthEndpoints();

if (app.Services.GetRequiredService<IOptions<JwtOptions>>().Value.EnableDevTokenEndpoint)
{
    app.MapDevTokenEndpoints();
}

app.MapAuctionEndpoints();
app.MapBidEndpoints();
app.MapMyActivityEndpoints();
app.MapHub<AuctionHub>(AuctionHub.Route);

await app.RunAsync();

public partial class Program;
