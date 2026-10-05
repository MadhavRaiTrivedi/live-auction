using LiveAuction.Application.Auctions;
using LiveAuction.Application.Bidding;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using LiveAuction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveAuction.Infrastructure.Auctions;

internal sealed class AuctionReader(AuctionDbContext db) : IAuctionReader
{
    private const string LikeEscapeCharacter = "\\";

    private static readonly AuctionStatus[] OpenStatuses = [AuctionStatus.Live, AuctionStatus.Scheduled];

    public Task<Auction?> FindAsync(Guid auctionId, CancellationToken cancellationToken) =>
        db.Auctions.AsNoTracking().SingleOrDefaultAsync(auction => auction.Id == auctionId, cancellationToken);

    public async Task<IReadOnlyList<Auction>> SearchAsync(
        ListAuctionsQuery query,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var auctions = db.Auctions.AsNoTracking();
        auctions = query.Status is { } status
            ? auctions.Where(auction => auction.Status == status)
            : auctions.Where(auction => OpenStatuses.Contains(auction.Status));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            auctions = auctions.Where(auction => EF.Functions.ILike(auction.Title, pattern, LikeEscapeCharacter));
        }

        return await auctions
            .OrderBy(auction => auction.EndsAt)
            .ThenBy(auction => auction.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bid>> ListLatestBidsAsync(Guid auctionId, int limit, CancellationToken cancellationToken) =>
        await db.Bids.AsNoTracking()
            .Where(bid => bid.AuctionId == auctionId)
            .OrderByDescending(bid => bid.Sequence)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Auction>> ListBySellerAsync(Guid sellerId, int limit, CancellationToken cancellationToken) =>
        await db.Auctions.AsNoTracking()
            .Where(auction => auction.SellerId == sellerId)
            .OrderByDescending(auction => auction.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BidderAuction>> ListByBidderAsync(
        Guid bidderId,
        int limit,
        CancellationToken cancellationToken)
    {
        var highestBids = db.Bids
            .Where(bid => bid.BidderId == bidderId)
            .GroupBy(bid => bid.AuctionId)
            .Select(bids => new { AuctionId = bids.Key, HighestBidInPaise = bids.Max(bid => bid.AmountInPaise) });

        var rows = await db.Auctions.AsNoTracking()
            .Join(highestBids, auction => auction.Id, highest => highest.AuctionId, (auction, highest) => new { auction, highest.HighestBidInPaise })
            .OrderByDescending(row => row.auction.EndsAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new BidderAuction(row.auction, row.HighestBidInPaise))];
    }

    private static string EscapeLikePattern(string text) =>
        text.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter)
            .Replace("%", LikeEscapeCharacter + "%")
            .Replace("_", LikeEscapeCharacter + "_");
}
