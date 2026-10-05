using LiveAuction.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace LiveAuction.Api.Realtime;

internal sealed class SignalRAuctionNotifier(
    IHubContext<AuctionHub, IAuctionClient> hub,
    ILogger<SignalRAuctionNotifier> logger) : IAuctionNotifier
{
    // The change is already committed. If Redis is unreachable the request must still succeed;
    // watchers catch up from the snapshot they get when they reconnect.
    public async Task AuctionChangedAsync(AuctionUpdate update, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Group(HubGroups.Auction(update.AuctionId)).AuctionUpdated(update);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not push update for auction {AuctionId}", update.AuctionId);
        }
    }

    public async Task OutbidAsync(Guid bidderId, OutbidNotice notice, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.User(bidderId.ToString()).Outbid(notice);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not send outbid notice for auction {AuctionId}", notice.AuctionId);
        }
    }
}
