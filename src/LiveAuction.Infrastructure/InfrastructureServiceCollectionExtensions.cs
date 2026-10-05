using LiveAuction.Application.Auctions;
using LiveAuction.Application.Persistence;
using LiveAuction.Infrastructure.Auctions;
using LiveAuction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LiveAuction.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AuctionDbContext.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{AuctionDbContext.ConnectionStringName}' is not configured.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddDbContext<AuctionDbContext>((serviceProvider, options) => options
            .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IAuctionReader, AuctionReader>();

        return services;
    }
}
