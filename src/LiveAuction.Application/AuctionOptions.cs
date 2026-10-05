using LiveAuction.Application.Errors;
using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application;

public sealed class AuctionOptions
{
    public const string SectionName = "Auctions";

    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromDays(30);

    public void EnsureAllowed(AuctionTerms terms)
    {
        if (terms.EndsAt - terms.StartsAt > MaxDuration)
        {
            throw new InvalidRequestException($"An auction can run for at most {MaxDuration.TotalDays} days.");
        }
    }
}
