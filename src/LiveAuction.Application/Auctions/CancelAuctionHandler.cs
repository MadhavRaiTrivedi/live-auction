using LiveAuction.Application.Errors;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Persistence;
using LiveAuction.Application.Security;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

public sealed class CancelAuctionHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IAuctionNotifier notifier,
    BiddingRules rules,
    TimeProvider clock)
{
    public async Task<AuctionResponse> HandleAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var requester = requestContext.Requester;

        var auction = await auctions.FindAsync(auctionId, cancellationToken)
            ?? throw new NotFoundException("Auction", auctionId);

        if (requester.IsAdmin)
        {
            auction.CancelByAdmin(now);
        }
        else
        {
            requester.EnsureIsSellerOf(auction);
            auction.CancelBySeller(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.AuctionChangedAsync(AuctionUpdate.From(auction, [], rules.Increments, now), cancellationToken);
        return AuctionResponse.From(auction, requester, rules.Increments, now);
    }
}
