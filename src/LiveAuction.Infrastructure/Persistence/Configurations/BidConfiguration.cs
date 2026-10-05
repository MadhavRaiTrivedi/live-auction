using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveAuction.Infrastructure.Persistence.Configurations;

internal sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public const string SequenceIndexName = "ix_bids_auction_id_sequence";

    private const int KindMaxLength = 16;

    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.HasKey(bid => bid.Id);
        builder.Property(bid => bid.Id).ValueGeneratedNever();
        builder.Property(bid => bid.Kind).HasConversion<string>().HasMaxLength(KindMaxLength);

        builder.HasOne<Auction>().WithMany().HasForeignKey(bid => bid.AuctionId).OnDelete(DeleteBehavior.Restrict);

        // Second line of defence: two bids can never take the same place in an auction's sequence.
        builder.HasIndex(bid => new { bid.AuctionId, bid.Sequence }).IsUnique().HasDatabaseName(SequenceIndexName);
        builder.HasIndex(bid => new { bid.BidderId, bid.AuctionId });
    }
}
