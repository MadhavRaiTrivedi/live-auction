using System.Threading.RateLimiting;
using LiveAuction.Api.Security;
using Microsoft.Extensions.Options;

namespace LiveAuction.Api.Http;

internal static class RateLimitingExtensions
{
    public const string BidsPolicy = "bids";

    // A token bucket per user lets a short burst through (a bidder reacting in the last seconds)
    // while capping the sustained rate. Limits are per API instance.
    public static IServiceCollection AddBidRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<BidRateLimitOptions>().BindConfiguration(BidRateLimitOptions.SectionName);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(BidsPolicy, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<BidRateLimitOptions>>().Value;
                var userId = httpContext.User.FindFirst(AuctionClaimTypes.UserId)?.Value ?? string.Empty;
                return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = limits.TokenLimit,
                    TokensPerPeriod = limits.TokensPerPeriod,
                    ReplenishmentPeriod = limits.ReplenishmentPeriod,
                    QueueLimit = 0,
                    AutoReplenishment = true,
                });
            });
        });

        return services;
    }
}
