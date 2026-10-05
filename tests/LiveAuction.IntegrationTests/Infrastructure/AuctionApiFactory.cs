using LiveAuction.Application.Lifecycle;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LiveAuction.IntegrationTests.Infrastructure;

// One factory is one API instance. Tests that need two instances create a second factory.
public sealed class AuctionApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const int LifecycleBatchSize = 1_000;

    public async ValueTask InitializeAsync()
    {
        await TestContainers.EnsureStartedAsync();
        using var warmUp = CreateClient();
    }

    // The background job is off in tests so each test decides when auctions start and close.
    public async Task<int> StartDueAuctionsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<StartDueAuctionsHandler>()
            .HandleAsync(LifecycleBatchSize, TestContext.Current.CancellationToken);
    }

    public async Task<int> CloseEndedAuctionsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CloseEndedAuctionsHandler>()
            .HandleAsync(LifecycleBatchSize, TestContext.Current.CancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Auctions", TestContainers.Postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", TestContainers.Redis.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("Jwt:EnableDevTokenEndpoint", "true");
        builder.UseSetting("Lifecycle:IsEnabled", "false");
        builder.UseSetting("RateLimiting:Bids:TokenLimit", "10000");
        builder.UseSetting("RateLimiting:Bids:TokensPerPeriod", "10000");
    }
}
