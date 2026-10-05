using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Auctions;

public sealed record SellerAuctionResponse(
    Guid Id,
    string Title,
    AuctionStatus Status,
    long StartingPriceInPaise,
    long? ReservePriceInPaise,
    long? CurrentPriceInPaise,
    int BidCount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid? WinnerId)
{
    public static SellerAuctionResponse From(Auction auction) =>
        new(
            auction.Id,
            auction.Title,
            auction.Status,
            auction.StartingPriceInPaise,
            auction.ReservePriceInPaise,
            auction.CurrentPriceInPaise,
            auction.BidCount,
            auction.StartsAt,
            auction.EndsAt,
            auction.WinnerId);
}
