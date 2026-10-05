using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Bidding;

public sealed record BidResponse(int Sequence, string BidderAlias, long AmountInPaise, BidKind Kind, DateTimeOffset PlacedAt)
{
    public static BidResponse From(Bid bid) =>
        new(bid.Sequence, Bidding.BidderAlias.For(bid.AuctionId, bid.BidderId), bid.AmountInPaise, bid.Kind, bid.PlacedAt);
}
