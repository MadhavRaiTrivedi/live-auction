namespace LiveAuction.Domain.Bidding;

public sealed class BidIncrementTable
{
    private readonly BidIncrementTier[] _tiers;

    public BidIncrementTable(IEnumerable<BidIncrementTier> tiers)
    {
        _tiers = [.. tiers];
        if (_tiers.Length == 0 || _tiers[^1].UpToInPaise is not null)
        {
            throw new ArgumentException("The last increment tier must have no upper limit.", nameof(tiers));
        }

        if (_tiers.Any(tier => tier.IncrementInPaise <= 0))
        {
            throw new ArgumentException("Every increment must be greater than zero.", nameof(tiers));
        }

        var limits = _tiers[..^1].Select(tier => tier.UpToInPaise).ToArray();
        if (limits.Any(limit => limit is null) || !limits.SequenceEqual(limits.Order()) || limits.Distinct().Count() != limits.Length)
        {
            throw new ArgumentException("Tier limits must be set and strictly ascending.", nameof(tiers));
        }
    }

    public long IncrementFor(long priceInPaise) =>
        _tiers.First(tier => tier.UpToInPaise is null || priceInPaise <= tier.UpToInPaise).IncrementInPaise;
}
