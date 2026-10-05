using LiveAuction.Domain.Auctions;
using LiveAuction.IntegrationTests.Infrastructure;

namespace LiveAuction.IntegrationTests.Realtime;

public class RealtimeTests(AuctionApiFactory factory) : IClassFixture<AuctionApiFactory>
{
    [Fact]
    public async Task Watch_ReturnsCurrentStateIncludingEarlierBids()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller);
        await bidder.BidAsync(auction.Id, auction.MinimumNextBidInPaise);
        await using var watcher = await HubClient.ConnectAsync(factory, seller);

        var snapshot = await watcher.WatchAsync(auction.Id);

        snapshot.Status.ShouldBe(AuctionStatus.Live);
        snapshot.BidCount.ShouldBe(1);
        snapshot.Bids.ShouldHaveSingleItem().AmountInPaise.ShouldBe(auction.MinimumNextBidInPaise);
    }

    [Fact]
    public async Task Bid_IsPushedToWatchers()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller);
        await using var watcher = await HubClient.ConnectAsync(factory, seller);
        await watcher.WatchAsync(auction.Id);

        await bidder.BidAsync(auction.Id, 25_000);

        var update = await watcher.NextUpdateAsync();
        update.AuctionId.ShouldBe(auction.Id);
        update.CurrentPriceInPaise.ShouldBe(25_000);
        update.Bids.ShouldHaveSingleItem().Sequence.ShouldBe(1);
    }

    [Fact]
    public async Task Bid_OutbiddingLeader_SendsPrivateNoticeToPreviousLeader()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var alice = await AuctionClient.UserAsync(factory);
        var bob = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);
        await alice.BidAsync(auction.Id, 10_000);
        await using var aliceConnection = await HubClient.ConnectAsync(factory, alice);

        await bob.BidAsync(auction.Id, 12_000);

        var notice = await aliceConnection.NextOutbidNoticeAsync();
        notice.AuctionId.ShouldBe(auction.Id);
        notice.CurrentPriceInPaise.ShouldBe(12_000);
    }

    [Fact]
    public async Task Bid_OnOneInstance_ReachesWatcherOnAnotherThroughRedis()
    {
        await using var otherInstance = new AuctionApiFactory();
        await otherInstance.InitializeAsync();
        var seller = await AuctionClient.UserAsync(factory);
        var bidderOnOtherInstance = await AuctionClient.UserAsync(otherInstance);
        var auction = await LiveAuctions.OpenAsync(factory, seller);
        await using var watcher = await HubClient.ConnectAsync(factory, seller);
        await watcher.WatchAsync(auction.Id);

        await bidderOnOtherInstance.BidAsync(auction.Id, 30_000);

        (await watcher.NextUpdateAsync()).CurrentPriceInPaise.ShouldBe(30_000);
    }
}
