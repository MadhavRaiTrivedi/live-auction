namespace LiveAuction.Domain.Auctions;

public sealed record AuctionTerms(
    string Title,
    string Description,
    long StartingPriceInPaise,
    long? ReservePriceInPaise,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
