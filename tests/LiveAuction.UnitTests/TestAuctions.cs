using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.UnitTests;

internal static class TestAuctions
{
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    public static readonly TimeSpan SoftCloseWindow = TimeSpan.FromSeconds(30);

    public static readonly BiddingRules Rules = new(
        new BidIncrementTable(
        [
            new BidIncrementTier(Rupees(1_000), Rupees(10)),
            new BidIncrementTier(Rupees(10_000), Rupees(50)),
            new BidIncrementTier(null, Rupees(100)),
        ]),
        SoftCloseWindow,
        SoftCloseExtension: TimeSpan.FromSeconds(30));

    public static readonly Guid SellerId = Guid.NewGuid();

    public static long Rupees(long rupees) => rupees * 100;

    public static AuctionTerms Terms(long startingRupees = 100, long? reserveRupees = null, TimeSpan? duration = null) =>
        new(
            "Vintage camera",
            "Working condition, original lens.",
            Rupees(startingRupees),
            reserveRupees is { } reserve ? Rupees(reserve) : null,
            Now,
            Now + (duration ?? TimeSpan.FromHours(1)));

    public static Auction Scheduled(long startingRupees = 100, long? reserveRupees = null) =>
        Auction.Create(SellerId, Terms(startingRupees, reserveRupees), Now);

    public static Auction Live(long startingRupees = 100, long? reserveRupees = null)
    {
        var auction = Scheduled(startingRupees, reserveRupees);
        auction.Start(Now);
        return auction;
    }
}
