namespace LiveAuction.Application.Bidding;

// A settable class rather than the domain record: the configuration binder silently drops array
// elements it cannot construct, which would quietly remove the unbounded last tier.
public sealed class IncrementTierOptions
{
    public long? UpToInPaise { get; init; }

    public long IncrementInPaise { get; init; }
}
