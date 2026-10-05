using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Auctions;

public sealed record ReviseAuctionCommand(Guid AuctionId, AuctionTerms Terms);
