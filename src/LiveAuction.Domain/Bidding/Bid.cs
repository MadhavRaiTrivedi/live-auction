namespace LiveAuction.Domain.Bidding;

public sealed class Bid
{
    private Bid()
    {
    }

    public Guid Id { get; private set; }

    public Guid AuctionId { get; private set; }

    public int Sequence { get; private set; }

    public Guid BidderId { get; private set; }

    public long AmountInPaise { get; private set; }

    public BidKind Kind { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    internal static Bid Create(
        Guid auctionId,
        int sequence,
        Guid bidderId,
        long amountInPaise,
        BidKind kind,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            AuctionId = auctionId,
            Sequence = sequence,
            BidderId = bidderId,
            AmountInPaise = amountInPaise,
            Kind = kind,
            PlacedAt = now,
        };
}
