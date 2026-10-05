namespace LiveAuction.Api.Http;

public sealed class BidRateLimitOptions
{
    public const string SectionName = "RateLimiting:Bids";

    public int TokenLimit { get; init; } = 5;

    public int TokensPerPeriod { get; init; } = 5;

    public TimeSpan ReplenishmentPeriod { get; init; } = TimeSpan.FromSeconds(1);
}
