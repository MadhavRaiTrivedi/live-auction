using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;
using Microsoft.EntityFrameworkCore;

namespace LiveAuction.Infrastructure.Persistence;

public sealed class AuctionDbContext(DbContextOptions<AuctionDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "Auctions";

    public DbSet<Auction> Auctions => Set<Auction>();

    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension(PostgresExtensions.Trigram);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuctionDbContext).Assembly);
    }
}
