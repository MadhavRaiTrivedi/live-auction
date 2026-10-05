using LiveAuction.Application.Bidding;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

public interface IAuctionReader
{
    Task<Auction?> FindAsync(Guid auctionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Auction>> SearchAsync(
        ListAuctionsQuery query,
        int offset,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Bid>> ListLatestBidsAsync(Guid auctionId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<Auction>> ListBySellerAsync(Guid sellerId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<BidderAuction>> ListByBidderAsync(Guid bidderId, int limit, CancellationToken cancellationToken);
}
