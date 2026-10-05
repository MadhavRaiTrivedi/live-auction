using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

public interface IAuctionRepository
{
    Task<Auction?> FindAsync(Guid auctionId, CancellationToken cancellationToken);

    // Both skip auctions locked by another transaction, so every API instance can run the lifecycle job.
    Task<IReadOnlyList<Auction>> LockDueToStartAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<Auction>> LockDueToCloseAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

    void Add(Auction auction);

    void AddBids(IEnumerable<Bid> bids);
}
