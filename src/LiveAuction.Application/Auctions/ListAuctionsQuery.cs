using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Auctions;

public sealed record ListAuctionsQuery(string? Search, AuctionStatus? Status, int Page);
