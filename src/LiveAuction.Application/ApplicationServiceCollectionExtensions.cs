using LiveAuction.Application.Auctions;
using LiveAuction.Application.Bidding;
using LiveAuction.Application.Lifecycle;
using LiveAuction.Application.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LiveAuction.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<AuctionOptions>().BindConfiguration(AuctionOptions.SectionName);
        services.AddOptions<BiddingOptions>().BindConfiguration(BiddingOptions.SectionName).ValidateOnStart();
        services.AddSingleton<IValidateOptions<BiddingOptions>, BiddingOptionsValidator>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<BiddingOptions>>().Value.ToRules());

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuctionMetrics>();

        services.AddScoped<CreateAuctionHandler>();
        services.AddScoped<ReviseAuctionHandler>();
        services.AddScoped<CancelAuctionHandler>();
        services.AddScoped<GetAuctionHandler>();
        services.AddScoped<GetAuctionSnapshotHandler>();
        services.AddScoped<ListAuctionsHandler>();
        services.AddScoped<ListSellerAuctionsHandler>();

        services.AddScoped<PlaceBidHandler>();
        services.AddScoped<GetBidHistoryHandler>();
        services.AddScoped<ListBidderAuctionsHandler>();

        services.AddScoped<StartDueAuctionsHandler>();
        services.AddScoped<CloseEndedAuctionsHandler>();

        return services;
    }
}
