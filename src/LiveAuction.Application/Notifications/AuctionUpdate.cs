using LiveAuction.Application.Bidding;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Notifications;

public sealed record AuctionUpdate(
    Guid AuctionId,
    AuctionStatus Status,
    long? CurrentPriceInPaise,
    long MinimumNextBidInPaise,
    int BidCount,
    string? LeadingBidderAlias,
    bool IsReserveMet,
    DateTimeOffset EndsAt,
    IReadOnlyList<BidResponse> Bids,
    DateTimeOffset ServerTime)
{
    public static AuctionUpdate From(Auction auction, IEnumerable<Bid> bids, BidIncrementTable increments, DateTimeOffset now) =>
        new(
            auction.Id,
            auction.Status,
            auction.CurrentPriceInPaise,
            auction.MinimumNextBidInPaise(increments),
            auction.BidCount,
            auction.LeadingBidderId is { } leaderId ? BidderAlias.For(auction.Id, leaderId) : null,
            auction.IsReserveMet,
            auction.EndsAt,
            [.. bids.Select(BidResponse.From)],
            now);
}
