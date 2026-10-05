using System.Security.Cryptography;

namespace LiveAuction.Application.Bidding;

// Derived per auction, so other bidders can follow the bidding without learning who someone is
// or recognising the same person across auctions.
public static class BidderAlias
{
    private const int GuidSizeBytes = 16;
    private const int AliasSizeBytes = 3;

    public static string For(Guid auctionId, Guid bidderId)
    {
        Span<byte> input = stackalloc byte[GuidSizeBytes * 2];
        auctionId.TryWriteBytes(input[..GuidSizeBytes]);
        bidderId.TryWriteBytes(input[GuidSizeBytes..]);
        var hash = SHA256.HashData(input);
        return $"Bidder {Convert.ToHexString(hash, 0, AliasSizeBytes)}";
    }
}
