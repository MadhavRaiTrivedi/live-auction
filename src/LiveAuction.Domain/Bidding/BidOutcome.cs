namespace LiveAuction.Domain.Bidding;

public sealed record BidOutcome(IReadOnlyList<Bid> PlacedBids, bool IsBidderLeading, Guid? OutbidBidderId);
