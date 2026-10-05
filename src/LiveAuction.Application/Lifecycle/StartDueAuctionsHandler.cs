using LiveAuction.Application.Auctions;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Persistence;
using LiveAuction.Domain.Bidding;
using Microsoft.Extensions.Logging;

namespace LiveAuction.Application.Lifecycle;

public sealed class StartDueAuctionsHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IAuctionNotifier notifier,
    BiddingRules rules,
    TimeProvider clock,
    ILogger<StartDueAuctionsHandler> logger)
{
    public async Task<int> HandleAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var due = await auctions.LockDueToStartAsync(now, batchSize, cancellationToken);
        if (due.Count == 0)
        {
            return 0;
        }

        foreach (var auction in due)
        {
            auction.Start(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Started {AuctionCount} auctions", due.Count);

        foreach (var auction in due)
        {
            await notifier.AuctionChangedAsync(AuctionUpdate.From(auction, [], rules.Increments, now), cancellationToken);
        }

        return due.Count;
    }
}
