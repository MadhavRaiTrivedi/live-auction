namespace LiveAuction.Application.Notifications;

public sealed record OutbidNotice(Guid AuctionId, string Title, long CurrentPriceInPaise, long MinimumNextBidInPaise);
