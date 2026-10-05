using System.Net;
using LiveAuction.Application.Auctions;
using LiveAuction.Application.Bidding;
using LiveAuction.Domain;
using LiveAuction.Domain.Bidding;
using LiveAuction.IntegrationTests.Infrastructure;

namespace LiveAuction.IntegrationTests.Bidding;

public class BiddingTests(AuctionApiFactory factory) : IClassFixture<AuctionApiFactory>
{
    [Fact]
    public async Task Bid_AtMinimum_LeadsAndAppearsInHistoryUnderAlias()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);

        var placed = await AuctionClient.ReadAsync<PlaceBidResponse>(await bidder.BidAsync(auction.Id, 10_000));

        placed.IsLeading.ShouldBeTrue();
        placed.Auction.MinimumNextBidInPaise.ShouldBe(11_000);
        var history = await seller.GetBidsAsync(auction.Id);
        var bid = history.ShouldHaveSingleItem();
        bid.BidderAlias.ShouldBe(placed.Auction.YourAlias);
        bid.BidderAlias.ShouldNotContain(bidder.UserId.ToString());
    }

    [Fact]
    public async Task Bid_BelowMinimum_ReturnsUnprocessableWithReason()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);

        var response = await bidder.BidAsync(auction.Id, 9_999);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await AuctionClient.ReadProblemAsync(response)).Code.ShouldBe(nameof(DomainErrorCode.BidTooLow));
    }

    [Fact]
    public async Task Bid_OnScheduledAuction_ReturnsConflict()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var created = await AuctionClient.ReadAsync<AuctionResponse>(
            await seller.CreateAuctionAsync());

        var response = await bidder.BidAsync(created.Id, 10_000);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await AuctionClient.ReadProblemAsync(response)).Code.ShouldBe(nameof(DomainErrorCode.AuctionNotLive));
    }

    [Fact]
    public async Task ProxyBid_LeaderDefendsAutomaticallyAndHidesMaximum()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var alice = await AuctionClient.UserAsync(factory);
        var bob = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);
        await AuctionClient.ReadAsync<PlaceBidResponse>(await alice.ProxyBidAsync(auction.Id, 50_000));

        var challenge = await AuctionClient.ReadAsync<PlaceBidResponse>(await bob.BidAsync(auction.Id, 20_000));

        challenge.IsLeading.ShouldBeFalse();
        challenge.PlacedBids.Select(bid => (bid.AmountInPaise, bid.Kind)).ShouldBe(
        [
            (20_000L, BidKind.Manual),
            (21_000L, BidKind.Auto),
        ]);
        (await bob.GetAuctionAsync(auction.Id)).YourMaxBidInPaise.ShouldBeNull();
        (await alice.GetAuctionAsync(auction.Id)).YourMaxBidInPaise.ShouldBe(50_000);
    }

    [Fact]
    public async Task ConcurrentBids_AtSameAmount_ExactlyOneWins()
    {
        const int bidderCount = 20;
        var seller = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);
        var bidders = await Task.WhenAll(Enumerable.Range(0, bidderCount).Select(_ => AuctionClient.UserAsync(factory)));

        var responses = await Task.WhenAll(bidders.Select(bidder => bidder.BidAsync(auction.Id, 10_000)));

        responses.Count(response => response.IsSuccessStatusCode).ShouldBe(1);
        responses.Where(response => !response.IsSuccessStatusCode)
            .ShouldAllBe(response => response.StatusCode == HttpStatusCode.UnprocessableEntity
                || response.StatusCode == HttpStatusCode.Conflict);
        var winner = bidders[Array.FindIndex(responses, response => response.IsSuccessStatusCode)];
        var final = await winner.GetAuctionAsync(auction.Id);
        final.BidCount.ShouldBe(1);
        final.IsLeading.ShouldBeTrue();
    }

    [Fact]
    public async Task ConcurrentBids_AtRisingAmounts_KeepSequenceGapFreeAndPriceAtHighestAccepted()
    {
        const int bidderCount = 30;
        const long stepInPaise = 20_000;
        var seller = await AuctionClient.UserAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller, startingPriceInPaise: 10_000);
        var bidders = await Task.WhenAll(Enumerable.Range(0, bidderCount).Select(_ => AuctionClient.UserAsync(factory)));

        var responses = await Task.WhenAll(bidders.Select((bidder, index) =>
            bidder.BidAsync(auction.Id, 10_000 + (index * stepInPaise))));

        var accepted = await Task.WhenAll(responses.Where(response => response.IsSuccessStatusCode)
            .Select(AuctionClient.ReadAsync<PlaceBidResponse>));
        var history = (await seller.GetBidsAsync(auction.Id)).OrderBy(bid => bid.Sequence).ToList();
        history.Count.ShouldBe(accepted.Length);
        history.Select(bid => bid.Sequence).ShouldBe(Enumerable.Range(1, history.Count));
        history.Select(bid => bid.AmountInPaise).ShouldBe(history.Select(bid => bid.AmountInPaise).Order());
        (await seller.GetAuctionAsync(auction.Id)).CurrentPriceInPaise.ShouldBe(history[^1].AmountInPaise);
    }
}
