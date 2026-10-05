using LiveAuction.Application.Errors;
using LiveAuction.Application.Notifications;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

// Sent to a watcher when it starts or resumes watching, so it never depends on updates it missed.
public sealed class GetAuctionSnapshotHandler(IAuctionReader reader, BiddingRules rules, TimeProvider clock)
{
    private const int RecentBidCount = 20;

    public async Task<AuctionUpdate> HandleAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var auction = await reader.FindAsync(auctionId, cancellationToken)
            ?? throw new NotFoundException("Auction", auctionId);
        var recentBids = await reader.ListLatestBidsAsync(auctionId, RecentBidCount, cancellationToken);
        return AuctionUpdate.From(auction, recentBids, rules.Increments, clock.GetUtcNow());
    }
}
