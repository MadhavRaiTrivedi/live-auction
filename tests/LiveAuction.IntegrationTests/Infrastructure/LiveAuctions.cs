using LiveAuction.Application.Auctions;

namespace LiveAuction.IntegrationTests.Infrastructure;

internal static class LiveAuctions
{
    public static async Task<AuctionResponse> OpenAsync(
        AuctionApiFactory factory,
        AuctionClient seller,
        long startingPriceInPaise = 10_000,
        long? reservePriceInPaise = null,
        TimeSpan? duration = null)
    {
        var created = await AuctionClient.ReadAsync<AuctionResponse>(
            await seller.CreateAuctionAsync(startingPriceInPaise, reservePriceInPaise, duration));
        await factory.StartDueAuctionsAsync();
        return await seller.GetAuctionAsync(created.Id);
    }
}
