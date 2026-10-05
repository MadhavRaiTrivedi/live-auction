using LiveAuction.Domain.Bidding;
using static LiveAuction.UnitTests.TestAuctions;

namespace LiveAuction.UnitTests.Bidding;

public class BidIncrementTableTests
{
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1_000, 10)]
    [InlineData(1_001, 50)]
    [InlineData(10_000, 50)]
    [InlineData(250_000, 100)]
    public void IncrementFor_UsesTheTierThePriceFallsIn(long priceInRupees, long expectedIncrementInRupees)
    {
        Rules.Increments.IncrementFor(Rupees(priceInRupees)).ShouldBe(Rupees(expectedIncrementInRupees));
    }

    [Fact]
    public void Constructor_WhenLastTierHasALimit_Throws()
    {
        Should.Throw<ArgumentException>(() => new BidIncrementTable([new BidIncrementTier(1_000, 10)]));
    }

    [Fact]
    public void Constructor_WhenLimitsAreNotAscending_Throws()
    {
        Should.Throw<ArgumentException>(() => new BidIncrementTable(
        [
            new BidIncrementTier(5_000, 10),
            new BidIncrementTier(1_000, 50),
            new BidIncrementTier(null, 100),
        ]));
    }

    [Fact]
    public void Constructor_WhenAnIncrementIsNotPositive_Throws()
    {
        Should.Throw<ArgumentException>(() => new BidIncrementTable([new BidIncrementTier(null, 0)]));
    }
}
