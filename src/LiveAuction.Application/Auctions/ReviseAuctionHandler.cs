using LiveAuction.Application.Errors;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Persistence;
using LiveAuction.Application.Security;
using LiveAuction.Domain.Bidding;
using Microsoft.Extensions.Options;

namespace LiveAuction.Application.Auctions;

public sealed class ReviseAuctionHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IAuctionNotifier notifier,
    IOptions<AuctionOptions> options,
    BiddingRules rules,
    TimeProvider clock)
{
    public async Task<AuctionResponse> HandleAsync(ReviseAuctionCommand command, CancellationToken cancellationToken)
    {
        options.Value.EnsureAllowed(command.Terms);
        var now = clock.GetUtcNow();
        var requester = requestContext.Requester;

        var auction = await auctions.FindAsync(command.AuctionId, cancellationToken)
            ?? throw new NotFoundException("Auction", command.AuctionId);
        requester.EnsureIsSellerOf(auction);

        auction.Revise(command.Terms, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.AuctionChangedAsync(AuctionUpdate.From(auction, [], rules.Increments, now), cancellationToken);
        return AuctionResponse.From(auction, requester, rules.Increments, now);
    }
}
