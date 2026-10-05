using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Bidding;

public sealed class BiddingOptions
{
    public const string SectionName = "Bidding";

    public BidIncrementTier[] IncrementTiers { get; init; } = [];

    public TimeSpan SoftCloseWindow { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan SoftCloseExtension { get; init; } = TimeSpan.FromSeconds(30);

    public int MaxConcurrencyAttempts { get; init; } = 5;

    public BiddingRules ToRules() =>
        new(new BidIncrementTable(IncrementTiers), SoftCloseWindow, SoftCloseExtension);
}
