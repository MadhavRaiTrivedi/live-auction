using LiveAuction.Application.Errors;
using LiveAuction.Application.Security;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

public sealed class GetAuctionHandler(
    IAuctionReader reader,
    IRequestContext requestContext,
    BiddingRules rules,
    TimeProvider clock)
{
    public async Task<AuctionResponse> HandleAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var auction = await reader.FindAsync(auctionId, cancellationToken)
            ?? throw new NotFoundException("Auction", auctionId);
        return AuctionResponse.From(auction, requestContext.Requester, rules.Increments, clock.GetUtcNow());
    }
}
