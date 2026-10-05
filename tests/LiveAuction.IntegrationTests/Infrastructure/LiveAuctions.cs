using LiveAuction.Application.Auctions;
using LiveAuction.Domain.Auctions;

namespace LiveAuction.IntegrationTests.Infrastructure;

internal static class LiveAuctions
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    // Test classes run in parallel and each runs the start job. If another class's job holds this
    // auction's row lock, SKIP LOCKED skips it here, so keep going until this auction is live.
    public static async Task<AuctionResponse> OpenAsync(
        AuctionApiFactory factory,
        AuctionClient seller,
        long startingPriceInPaise = 10_000,
        long? reservePriceInPaise = null,
        TimeSpan? duration = null)
    {
        var created = await AuctionClient.ReadAsync<AuctionResponse>(
            await seller.CreateAuctionAsync(startingPriceInPaise, reservePriceInPaise, duration));

        var deadline = DateTimeOffset.UtcNow + StartTimeout;
        while (true)
        {
            await factory.StartDueAuctionsAsync();
            var auction = await seller.GetAuctionAsync(created.Id);
            if (auction.Status == AuctionStatus.Live)
            {
                return auction;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException($"Auction {created.Id} did not go live within {StartTimeout}.");
            }

            await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
        }
    }
}
