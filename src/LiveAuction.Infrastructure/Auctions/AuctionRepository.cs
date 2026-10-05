using LiveAuction.Application.Auctions;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using LiveAuction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveAuction.Infrastructure.Auctions;

internal sealed class AuctionRepository(AuctionDbContext db) : IAuctionRepository
{
    public Task<Auction?> FindAsync(Guid auctionId, CancellationToken cancellationToken) =>
        db.Auctions.SingleOrDefaultAsync(auction => auction.Id == auctionId, cancellationToken);

    public async Task<IReadOnlyList<Auction>> LockDueToStartAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        EnsureInTransaction();
        var scheduled = AuctionStatus.Scheduled.ToString();

        // xmin is a system column, so SELECT * leaves it out; EF needs it for the row version.
        return await db.Auctions
            .FromSql(
                $"""
                SELECT *, xmin FROM auctions
                WHERE status = {scheduled} AND starts_at <= {now}
                ORDER BY starts_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Auction>> LockDueToCloseAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        EnsureInTransaction();
        var live = AuctionStatus.Live.ToString();

        return await db.Auctions
            .FromSql(
                $"""
                SELECT *, xmin FROM auctions
                WHERE status = {live} AND ends_at <= {now}
                ORDER BY ends_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
    }

    public void Add(Auction auction) => db.Auctions.Add(auction);

    public void AddBids(IEnumerable<Bid> bids) => db.Bids.AddRange(bids);

    private void EnsureInTransaction()
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Row locks are only held inside a transaction. Begin one first.");
        }
    }
}
