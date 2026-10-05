using LiveAuction.Application.Auctions;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Observability;
using LiveAuction.Application.Persistence;
using LiveAuction.Domain.Bidding;
using Microsoft.Extensions.Logging;

namespace LiveAuction.Application.Lifecycle;

public sealed class CloseEndedAuctionsHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IAuctionNotifier notifier,
    BiddingRules rules,
    AuctionMetrics metrics,
    TimeProvider clock,
    ILogger<CloseEndedAuctionsHandler> logger)
{
    public async Task<int> HandleAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var ended = await auctions.LockDueToCloseAsync(now, batchSize, cancellationToken);
        if (ended.Count == 0)
        {
            return 0;
        }

        foreach (var auction in ended)
        {
            auction.Close(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Closed {AuctionCount} auctions", ended.Count);

        foreach (var auction in ended)
        {
            metrics.AuctionClosed(auction.Status);
            await notifier.AuctionChangedAsync(AuctionUpdate.From(auction, [], rules.Increments, now), cancellationToken);
        }

        return ended.Count;
    }
}
