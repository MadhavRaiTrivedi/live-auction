namespace LiveAuction.Domain.Bidding;

public sealed record BiddingRules(BidIncrementTable Increments, TimeSpan SoftCloseWindow, TimeSpan SoftCloseExtension);
