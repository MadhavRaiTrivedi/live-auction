using System.Net;
using LiveAuction.Application.Bidding;
using LiveAuction.Domain.Auctions;
using LiveAuction.IntegrationTests.Infrastructure;

namespace LiveAuction.IntegrationTests.Lifecycle;

public class LifecycleTests(AuctionApiFactory factory) : IClassFixture<AuctionApiFactory>
{
    private static readonly TimeSpan ShortAuction = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Close_AfterEndTime_SellsToLeaderWhenReserveMet()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, 10_000, reservePriceInPaise: 15_000, ShortAuction);
        await bidder.PlaceBidAsync(auction.Id, 15_000);

        await WaitUntilEndedAsync(seller, auction.Id);
        await factory.CloseEndedAuctionsAsync();

        var closed = await seller.GetAuctionAsync(auction.Id);
        closed.Status.ShouldBe(AuctionStatus.Sold);
        (await bidder.GetAsync<List<BidderAuctionResponse>>("/api/me/bids"))
            .ShouldContain(row => row.AuctionId == auction.Id && row.Standing == BidStanding.Won);
    }

    [Fact]
    public async Task Close_FromTwoInstancesAtOnce_ClosesEachAuctionOnce()
    {
        await using var secondInstance = new AuctionApiFactory();
        await secondInstance.InitializeAsync();
        var seller = await AuctionClient.UserAsync(factory);
        var auctions = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            LiveAuctions.OpenAsync(factory, seller, duration: ShortAuction)));
        await WaitUntilEndedAsync(seller, auctions.Max(auction => auction.EndsAt));

        // A second close of the same auction would throw InvalidStatusTransition and fail the test.
        await Task.WhenAll(factory.CloseEndedAuctionsAsync(), secondInstance.CloseEndedAuctionsAsync());

        foreach (var auction in auctions)
        {
            (await seller.GetAuctionAsync(auction.Id)).Status.ShouldBe(AuctionStatus.Unsold);
        }
    }

    [Fact]
    public async Task Bid_AfterEndTimeBeforeCloseJobRan_IsRejected()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, duration: ShortAuction);
        await WaitUntilEndedAsync(seller, auction.Id);

        var response = await bidder.BidAsync(auction.Id, auction.MinimumNextBidInPaise);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await seller.GetAuctionAsync(auction.Id)).Status.ShouldBe(AuctionStatus.Live);
    }

    private static async Task WaitUntilEndedAsync(AuctionClient client, Guid auctionId) =>
        await WaitUntilEndedAsync(client, (await client.GetAuctionAsync(auctionId)).EndsAt);

    private static async Task WaitUntilEndedAsync(AuctionClient client, DateTimeOffset endsAt)
    {
        var remaining = endsAt - DateTimeOffset.UtcNow;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining + TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }
    }
}
