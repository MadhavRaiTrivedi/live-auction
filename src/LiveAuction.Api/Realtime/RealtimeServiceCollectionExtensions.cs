using System.Text.Json.Serialization;
using LiveAuction.Application.Notifications;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace LiveAuction.Api.Realtime;

internal static class RealtimeServiceCollectionExtensions
{
    private const string RedisConnectionStringName = "Redis";
    private const string BackplaneChannelPrefix = "live-auction";

    public static IServiceCollection AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        var signalR = services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Without Redis a single instance still works; with it, every instance relays every group message.
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalR.AddStackExchangeRedis(redisConnectionString, options =>
                options.Configuration.ChannelPrefix = RedisChannel.Literal(BackplaneChannelPrefix));
        }

        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddSingleton<IAuctionNotifier, SignalRAuctionNotifier>();

        return services;
    }
}
