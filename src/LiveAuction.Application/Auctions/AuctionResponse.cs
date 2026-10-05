using LiveAuction.Application.Bidding;
using LiveAuction.Application.Security;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Auctions;

public sealed record AuctionResponse(
    Guid Id,
    Guid SellerId,
    string Title,
    string Description,
    AuctionStatus Status,
    long StartingPriceInPaise,
    long? ReservePriceInPaise,
    bool HasReserve,
    bool IsReserveMet,
    long? CurrentPriceInPaise,
    long MinimumNextBidInPaise,
    int BidCount,
    string? LeadingBidderAlias,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? ClosedAt,
    string YourAlias,
    bool IsSeller,
    bool IsLeading,
    long? YourMaxBidInPaise,
    DateTimeOffset ServerTime)
{
    public static AuctionResponse From(Auction auction, Requester requester, BidIncrementTable increments, DateTimeOffset now)
    {
        var isLeading = auction.LeadingBidderId == requester.UserId;
        return new AuctionResponse(
            auction.Id,
            auction.SellerId,
            auction.Title,
            auction.Description,
            auction.Status,
            auction.StartingPriceInPaise,
            requester.CanSeeReserveOf(auction) ? auction.ReservePriceInPaise : null,
            auction.ReservePriceInPaise is not null,
            auction.IsReserveMet,
            auction.CurrentPriceInPaise,
            auction.MinimumNextBidInPaise(increments),
            auction.BidCount,
            auction.LeadingBidderId is { } leaderId ? BidderAlias.For(auction.Id, leaderId) : null,
            auction.StartsAt,
            auction.EndsAt,
            auction.ClosedAt,
            BidderAlias.For(auction.Id, requester.UserId),
            requester.IsSellerOf(auction),
            isLeading,
            isLeading ? auction.LeaderMaxInPaise : null,
            now);
    }
}
