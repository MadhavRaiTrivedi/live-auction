using LiveAuction.Domain;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using static LiveAuction.UnitTests.TestAuctions;

namespace LiveAuction.UnitTests.Bidding;

public class BiddingTests
{
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    [Fact]
    public void PlaceManualBid_FirstBidAtStartingPrice_LeadsAtThatPrice()
    {
        var auction = Live(startingRupees: 100);

        var outcome = auction.PlaceManualBid(_alice, Rupees(100), Rules, Now);

        outcome.IsBidderLeading.ShouldBeTrue();
        outcome.PlacedBids.ShouldHaveSingleItem().Sequence.ShouldBe(1);
        auction.CurrentPriceInPaise.ShouldBe(Rupees(100));
        auction.MinimumNextBidInPaise(Rules.Increments).ShouldBe(Rupees(110));
    }

    [Fact]
    public void PlaceManualBid_BelowMinimumNextBid_Throws()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceManualBid(_alice, Rupees(100), Rules, Now);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceManualBid(_bob, Rupees(109), Rules, Now));

        exception.Code.ShouldBe(DomainErrorCode.BidTooLow);
        auction.LeadingBidderId.ShouldBe(_alice);
    }

    [Fact]
    public void PlaceManualBid_OutbiddingTheLeader_ReportsWhoWasOutbid()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceManualBid(_alice, Rupees(100), Rules, Now);

        var outcome = auction.PlaceManualBid(_bob, Rupees(110), Rules, Now);

        outcome.IsBidderLeading.ShouldBeTrue();
        outcome.OutbidBidderId.ShouldBe(_alice);
        auction.BidCount.ShouldBe(2);
    }

    [Fact]
    public void PlaceManualBid_ByTheLeader_Throws()
    {
        var auction = Live();
        auction.PlaceManualBid(_alice, Rupees(100), Rules, Now);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceManualBid(_alice, Rupees(200), Rules, Now));

        exception.Code.ShouldBe(DomainErrorCode.AlreadyLeading);
    }

    [Fact]
    public void PlaceManualBid_BySeller_Throws()
    {
        var auction = Live();

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceManualBid(SellerId, Rupees(100), Rules, Now));

        exception.Code.ShouldBe(DomainErrorCode.SellerCannotBid);
    }

    [Fact]
    public void PlaceManualBid_OnScheduledAuction_Throws()
    {
        var auction = Scheduled();

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceManualBid(_alice, Rupees(100), Rules, Now));

        exception.Code.ShouldBe(DomainErrorCode.AuctionNotLive);
    }

    [Fact]
    public void PlaceManualBid_AtEndTimeBeforeCloseJobRan_Throws()
    {
        var auction = Live();

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceManualBid(_alice, Rupees(100), Rules, auction.EndsAt));

        exception.Code.ShouldBe(DomainErrorCode.AuctionEnded);
        auction.Status.ShouldBe(AuctionStatus.Live);
    }

    [Fact]
    public void PlaceProxyBid_FirstBid_LeadsAtStartingPrice()
    {
        var auction = Live(startingRupees: 100);

        var outcome = auction.PlaceProxyBid(_alice, Rupees(500), Rules, Now);

        outcome.PlacedBids.ShouldHaveSingleItem().AmountInPaise.ShouldBe(Rupees(100));
        auction.LeaderMaxInPaise.ShouldBe(Rupees(500));
    }

    [Fact]
    public void PlaceManualBid_BelowLeadersMax_LeaderDefendsOneIncrementHigher()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(500), Rules, Now);

        var outcome = auction.PlaceManualBid(_bob, Rupees(200), Rules, Now);

        outcome.IsBidderLeading.ShouldBeFalse();
        outcome.PlacedBids.Select(bid => (bid.BidderId, bid.AmountInPaise, bid.Kind)).ShouldBe(
        [
            (_bob, Rupees(200), BidKind.Manual),
            (_alice, Rupees(210), BidKind.Auto),
        ]);
        auction.LeadingBidderId.ShouldBe(_alice);
    }

    [Fact]
    public void PlaceManualBid_EqualToLeadersMax_EarlierBidderKeepsLead()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(500), Rules, Now);

        auction.PlaceManualBid(_bob, Rupees(500), Rules, Now);

        auction.LeadingBidderId.ShouldBe(_alice);
        auction.CurrentPriceInPaise.ShouldBe(Rupees(500));
    }

    [Fact]
    public void PlaceManualBid_AboveLeadersMax_UsesUpTheProxyThenLeads()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(500), Rules, Now);

        var outcome = auction.PlaceManualBid(_bob, Rupees(600), Rules, Now);

        outcome.PlacedBids.Select(bid => (bid.BidderId, bid.AmountInPaise)).ShouldBe(
        [
            (_alice, Rupees(500)),
            (_bob, Rupees(600)),
        ]);
        outcome.OutbidBidderId.ShouldBe(_alice);
        auction.LeaderMaxInPaise.ShouldBe(Rupees(600));
    }

    [Fact]
    public void PlaceProxyBid_AgainstHigherProxy_LosesAtItsMax()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(500), Rules, Now);

        var outcome = auction.PlaceProxyBid(_bob, Rupees(300), Rules, Now);

        outcome.PlacedBids.Select(bid => (bid.BidderId, bid.AmountInPaise)).ShouldBe(
        [
            (_bob, Rupees(300)),
            (_alice, Rupees(310)),
        ]);
        auction.LeadingBidderId.ShouldBe(_alice);
    }

    [Fact]
    public void PlaceProxyBid_AgainstLowerProxy_WinsOneIncrementAboveIt()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(300), Rules, Now);

        var outcome = auction.PlaceProxyBid(_bob, Rupees(2_000), Rules, Now);

        outcome.PlacedBids.Select(bid => (bid.BidderId, bid.AmountInPaise, bid.Kind)).ShouldBe(
        [
            (_alice, Rupees(300), BidKind.Auto),
            (_bob, Rupees(310), BidKind.Proxy),
        ]);
        auction.LeaderMaxInPaise.ShouldBe(Rupees(2_000));
    }

    [Fact]
    public void PlaceProxyBid_ByLeaderWithHigherMax_RaisesMaxWithoutNewBid()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(300), Rules, Now);

        var outcome = auction.PlaceProxyBid(_alice, Rupees(900), Rules, Now);

        outcome.PlacedBids.ShouldBeEmpty();
        auction.CurrentPriceInPaise.ShouldBe(Rupees(100));
        auction.LeaderMaxInPaise.ShouldBe(Rupees(900));
    }

    [Fact]
    public void PlaceProxyBid_ByLeaderWithoutRaisingMax_Throws()
    {
        var auction = Live(startingRupees: 100);
        auction.PlaceProxyBid(_alice, Rupees(300), Rules, Now);

        var exception = Should.Throw<DomainRuleViolationException>(() =>
            auction.PlaceProxyBid(_alice, Rupees(300), Rules, Now));

        exception.Code.ShouldBe(DomainErrorCode.AlreadyLeading);
    }

    [Fact]
    public void Bids_AlwaysGetGapFreeSequenceAndNeverDecrease()
    {
        var auction = Live(startingRupees: 100);
        var carol = Guid.NewGuid();
        var placed = new List<Bid>();

        placed.AddRange(auction.PlaceProxyBid(_alice, Rupees(400), Rules, Now).PlacedBids);
        placed.AddRange(auction.PlaceManualBid(_bob, Rupees(250), Rules, Now).PlacedBids);
        placed.AddRange(auction.PlaceProxyBid(carol, Rupees(1_500), Rules, Now).PlacedBids);
        placed.AddRange(auction.PlaceManualBid(_bob, Rupees(1_200), Rules, Now).PlacedBids);

        placed.Select(bid => bid.Sequence).ShouldBe(Enumerable.Range(1, placed.Count));
        placed.Select(bid => bid.AmountInPaise).ShouldBe(placed.Select(bid => bid.AmountInPaise).Order());
        placed[^1].BidderId.ShouldBe(auction.LeadingBidderId!.Value);
    }

    [Fact]
    public void PlaceManualBid_InsideSoftCloseWindow_ExtendsEndTime()
    {
        var auction = Live();
        var originalEnd = auction.EndsAt;
        var lateBidTime = originalEnd - TimeSpan.FromSeconds(10);

        auction.PlaceManualBid(_alice, Rupees(100), Rules, lateBidTime);

        auction.EndsAt.ShouldBe(lateBidTime + Rules.SoftCloseExtension);
    }

    [Fact]
    public void PlaceManualBid_BeforeSoftCloseWindow_KeepsEndTime()
    {
        var auction = Live();
        var originalEnd = auction.EndsAt;

        auction.PlaceManualBid(_alice, Rupees(100), Rules, originalEnd - SoftCloseWindow - TimeSpan.FromSeconds(1));

        auction.EndsAt.ShouldBe(originalEnd);
    }
}
