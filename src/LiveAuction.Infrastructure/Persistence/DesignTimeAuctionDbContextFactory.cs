using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LiveAuction.Infrastructure.Persistence;

internal sealed class DesignTimeAuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    private const string MigrationsConnectionString =
        "Host=localhost;Database=live_auction;Username=postgres;Password=postgres";

    public AuctionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseNpgsql(MigrationsConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AuctionDbContext(options);
    }
}
