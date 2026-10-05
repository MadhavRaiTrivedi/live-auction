using LiveAuction.Application.Auctions;
using LiveAuction.Application.Errors;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Observability;
using LiveAuction.Application.Persistence;
using LiveAuction.Application.Security;
using LiveAuction.Domain;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using Microsoft.Extensions.Options;

namespace LiveAuction.Application.Bidding;

public sealed class PlaceBidHandler(
    IAuctionRepository auctions,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IAuctionNotifier notifier,
    IOptions<BiddingOptions> options,
    BiddingRules rules,
    AuctionMetrics metrics,
    TimeProvider clock)
{
    // Optimistic concurrency: the save fails if another bid updated the auction after we read it.
    // The bid is then resolved again against the new price, which may now reject it as too low.
    public async Task<PlaceBidResponse> HandleAsync(PlaceBidCommand command, CancellationToken cancellationToken)
    {
        var requester = requestContext.Requester;
        for (var attempt = 1; ; attempt++)
        {
            var now = clock.GetUtcNow();
            var auction = await auctions.FindAsync(command.AuctionId, cancellationToken)
                ?? throw new NotFoundException("Auction", command.AuctionId);

            var outcome = Resolve(auction, requester.UserId, command, now);
            auctions.AddBids(outcome.PlacedBids);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < options.Value.MaxConcurrencyAttempts)
            {
                metrics.BidConflict();
                unitOfWork.DiscardChanges();
                continue;
            }

            metrics.BidsAccepted(outcome.PlacedBids);
            await NotifyAsync(auction, outcome, now, cancellationToken);
            return new PlaceBidResponse(
                outcome.IsBidderLeading,
                [.. outcome.PlacedBids.Select(BidResponse.From)],
                AuctionResponse.From(auction, requester, rules.Increments, now));
        }
    }

    private BidOutcome Resolve(Auction auction, Guid bidderId, PlaceBidCommand command, DateTimeOffset now)
    {
        try
        {
            return command.Kind switch
            {
                BidKind.Manual => auction.PlaceManualBid(bidderId, command.AmountInPaise, rules, now),
                BidKind.Proxy => auction.PlaceProxyBid(bidderId, command.AmountInPaise, rules, now),
                _ => throw new InvalidRequestException($"{command.Kind} bids are placed by the system only."),
            };
        }
        catch (DomainRuleViolationException violation)
        {
            metrics.BidRejected(violation.Code);
            throw;
        }
    }

    private async Task NotifyAsync(Auction auction, BidOutcome outcome, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (outcome.PlacedBids.Count > 0)
        {
            await notifier.AuctionChangedAsync(
                AuctionUpdate.From(auction, outcome.PlacedBids, rules.Increments, now), cancellationToken);
        }

        if (outcome.OutbidBidderId is { } outbidBidderId)
        {
            var notice = new OutbidNotice(
                auction.Id, auction.Title, auction.CurrentPriceInPaise!.Value, auction.MinimumNextBidInPaise(rules.Increments));
            await notifier.OutbidAsync(outbidBidderId, notice, cancellationToken);
        }
    }
}
