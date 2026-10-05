using LiveAuction.Application.Lifecycle;
using Microsoft.Extensions.Options;

namespace LiveAuction.Api.Lifecycle;

// Runs in every API instance. The handlers lock due auctions with SKIP LOCKED, so instances share
// the work instead of repeating it. Each tick gets its own scope and a failed tick does not stop the next.
internal sealed class AuctionLifecycleJob(
    IServiceScopeFactory scopeFactory,
    IOptions<LifecycleOptions> options,
    ILogger<AuctionLifecycleJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var schedule = options.Value;
        using var timer = new PeriodicTimer(schedule.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<StartDueAuctionsHandler>()
                    .HandleAsync(schedule.BatchSize, stoppingToken);
                await scope.ServiceProvider.GetRequiredService<CloseEndedAuctionsHandler>()
                    .HandleAsync(schedule.BatchSize, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Auction lifecycle tick failed; retrying in {Interval}", schedule.Interval);
            }
        }
    }
}
