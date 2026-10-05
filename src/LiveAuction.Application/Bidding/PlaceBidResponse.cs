using LiveAuction.Application.Auctions;

namespace LiveAuction.Application.Bidding;

public sealed record PlaceBidResponse(bool IsLeading, IReadOnlyList<BidResponse> PlacedBids, AuctionResponse Auction);
