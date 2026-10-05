using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Bidding;

public sealed record PlaceBidCommand(Guid AuctionId, long AmountInPaise, BidKind Kind);
