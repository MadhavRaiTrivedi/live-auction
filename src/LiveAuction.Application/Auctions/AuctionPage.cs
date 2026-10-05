namespace LiveAuction.Application.Auctions;

public sealed record AuctionPage(IReadOnlyList<AuctionSummary> Items, int Page, bool HasMore);
