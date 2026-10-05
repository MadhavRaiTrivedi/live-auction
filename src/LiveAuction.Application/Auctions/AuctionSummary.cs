using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Auctions;

public sealed record AuctionSummary(
    Guid Id,
    string Title,
    AuctionStatus Status,
    long PriceInPaise,
    int BidCount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
{
    public static AuctionSummary From(Auction auction) =>
        new(
            auction.Id,
            auction.Title,
            auction.Status,
            auction.CurrentPriceInPaise ?? auction.StartingPriceInPaise,
            auction.BidCount,
            auction.StartsAt,
            auction.EndsAt);
}
