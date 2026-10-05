namespace LiveAuction.Api.Lifecycle;

public sealed class LifecycleOptions
{
    public const string SectionName = "Lifecycle";

    public bool IsEnabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);

    public int BatchSize { get; init; } = 100;
}
