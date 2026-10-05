using LiveAuction.Domain.Auctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiveAuction.Infrastructure.Persistence.Configurations;

internal sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    private const int StatusMaxLength = 16;

    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(auction => auction.Id);
        builder.Property(auction => auction.Id).ValueGeneratedNever();
        builder.Property(auction => auction.Title).HasMaxLength(AuctionTerms.MaxTitleLength);
        builder.Property(auction => auction.Description).HasMaxLength(AuctionTerms.MaxDescriptionLength);
        builder.Property(auction => auction.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);

        // Maps to PostgreSQL's xmin system column, which changes on every update of the row.
        builder.Property(auction => auction.Version).IsRowVersion();

        builder.HasIndex(auction => auction.StartsAt)
            .HasFilter($"status = '{AuctionStatus.Scheduled}'")
            .HasDatabaseName("ix_auctions_scheduled_starts_at");
        builder.HasIndex(auction => auction.EndsAt)
            .HasFilter($"status = '{AuctionStatus.Live}'")
            .HasDatabaseName("ix_auctions_live_ends_at");
        builder.HasIndex(auction => auction.SellerId);
        builder.HasIndex(auction => auction.Title)
            .HasMethod(PostgresExtensions.GinIndexMethod)
            .HasOperators(PostgresExtensions.TrigramIndexOperators)
            .HasDatabaseName("ix_auctions_title_trigram");
    }
}
