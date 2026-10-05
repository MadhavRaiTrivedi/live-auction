using LiveAuction.Application.Persistence;
using LiveAuction.Application.Security;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using Microsoft.Extensions.Options;

namespace LiveAuction.Application.Auctions;

public sealed class CreateAuctionHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IOptions<AuctionOptions> options,
    BiddingRules rules,
    TimeProvider clock)
{
    public async Task<AuctionResponse> HandleAsync(AuctionTerms terms, CancellationToken cancellationToken)
    {
        options.Value.EnsureAllowed(terms);
        var now = clock.GetUtcNow();
        var requester = requestContext.Requester;

        var auction = Auction.Create(requester.UserId, terms, now);
        auctions.Add(auction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuctionResponse.From(auction, requester, rules.Increments, now);
    }
}
