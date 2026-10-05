using LiveAuction.Domain;
using LiveAuction.Domain.Auctions;
using static LiveAuction.UnitTests.TestAuctions;

namespace LiveAuction.UnitTests.Auctions;

public class AuctionLifecycleTests
{
    [Fact]
    public void Create_StartsAsScheduledWithNoBids()
    {
        var auction = Scheduled();

        auction.Status.ShouldBe(AuctionStatus.Scheduled);
        auction.CurrentPriceInPaise.ShouldBeNull();
        auction.MinimumNextBidInPaise(Rules.Increments).ShouldBe(Rupees(100));
    }

    [Fact]
    public void Create_WhenReserveIsBelowStartingPrice_Throws()
    {
        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Auction.Create(SellerId, Terms(startingRupees: 100, reserveRupees: 50), Now));

        exception.Code.ShouldBe(DomainErrorCode.InvalidPrice);
    }

    [Fact]
    public void Create_WhenEndIsNotAfterStart_Throws()
    {
        var exception = Should.Throw<DomainRuleViolationException>(() =>
            Auction.Create(SellerId, Terms(duration: TimeSpan.Zero), Now));

        exception.Code.ShouldBe(DomainErrorCode.InvalidAuctionSchedule);
    }

    [Fact]
    public void Revise_AfterAuctionWentLive_Throws()
    {
        var auction = Live();

        var exception = Should.Throw<DomainRuleViolationException>(() => auction.Revise(Terms(startingRupees: 500), Now));

        exception.Code.ShouldBe(DomainErrorCode.AuctionNotEditable);
    }

    [Fact]
    public void Start_BeforeStartTime_Throws()
    {
        var auction = Scheduled();

        var exception = Should.Throw<DomainRuleViolationException>(() => auction.Start(Now.AddSeconds(-1)));

        exception.Code.ShouldBe(DomainErrorCode.AuctionNotStarted);
    }

    [Fact]
    public void Close_BeforeEndTime_Throws()
    {
        var auction = Live();

        var exception = Should.Throw<DomainRuleViolationException>(() => auction.Close(Now.AddMinutes(59)));

        exception.Code.ShouldBe(DomainErrorCode.AuctionNotEnded);
    }

    [Fact]
    public void Close_WithBidMeetingReserve_IsSoldToLeader()
    {
        var auction = Live(startingRupees: 100, reserveRupees: 150);
        var bidderId = Guid.NewGuid();
        auction.PlaceManualBid(bidderId, Rupees(150), Rules, Now);

        auction.Close(auction.EndsAt);

        auction.Status.ShouldBe(AuctionStatus.Sold);
        auction.WinnerId.ShouldBe(bidderId);
    }

    [Fact]
    public void Close_WithBidBelowReserve_IsUnsold()
    {
        var auction = Live(startingRupees: 100, reserveRupees: 150);
        auction.PlaceManualBid(Guid.NewGuid(), Rupees(120), Rules, Now);

        auction.Close(auction.EndsAt);

        auction.Status.ShouldBe(AuctionStatus.Unsold);
        auction.WinnerId.ShouldBeNull();
    }

    [Fact]
    public void Close_WithNoBids_IsUnsold()
    {
        var auction = Live();

        auction.Close(auction.EndsAt);

        auction.Status.ShouldBe(AuctionStatus.Unsold);
    }

    [Fact]
    public void Close_Twice_Throws()
    {
        var auction = Live();
        auction.Close(auction.EndsAt);

        var exception = Should.Throw<DomainRuleViolationException>(() => auction.Close(auction.EndsAt));

        exception.Code.ShouldBe(DomainErrorCode.InvalidStatusTransition);
    }

    [Fact]
    public void CancelBySeller_LiveAuctionWithBids_Throws()
    {
        var auction = Live();
        auction.PlaceManualBid(Guid.NewGuid(), Rupees(100), Rules, Now);

        var exception = Should.Throw<DomainRuleViolationException>(() => auction.CancelBySeller(Now));

        exception.Code.ShouldBe(DomainErrorCode.AuctionHasBids);
    }

    [Fact]
    public void CancelByAdmin_LiveAuctionWithBids_Cancels()
    {
        var auction = Live();
        auction.PlaceManualBid(Guid.NewGuid(), Rupees(100), Rules, Now);

        auction.CancelByAdmin(Now);

        auction.Status.ShouldBe(AuctionStatus.Cancelled);
        auction.WinnerId.ShouldBeNull();
    }
}
