using LiveAuction.Application.Auctions;
using LiveAuction.Application.Security;

namespace LiveAuction.Application.Bidding;

public sealed class ListBidderAuctionsHandler(IAuctionReader reader, IRequestContext requestContext)
{
    private const int MaxResults = 100;

    public async Task<IReadOnlyList<BidderAuctionResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var bidderId = requestContext.Requester.UserId;
        var auctions = await reader.ListByBidderAsync(bidderId, MaxResults, cancellationToken);
        return [.. auctions.Select(auction => BidderAuctionResponse.From(auction, bidderId))];
    }
}
