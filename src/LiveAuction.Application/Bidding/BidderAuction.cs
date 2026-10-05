using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Bidding;

public sealed record BidderAuction(Auction Auction, long HighestBidInPaise);
