using LiveAuction.Application.Auctions;
using LiveAuction.Application.Errors;

namespace LiveAuction.Application.Bidding;

public sealed class GetBidHistoryHandler(IAuctionReader reader)
{
    public const int MaxBids = 200;

    public async Task<IReadOnlyList<BidResponse>> HandleAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        _ = await reader.FindAsync(auctionId, cancellationToken) ?? throw new NotFoundException("Auction", auctionId);
        var bids = await reader.ListLatestBidsAsync(auctionId, MaxBids, cancellationToken);
        return [.. bids.Select(BidResponse.From)];
    }
}
