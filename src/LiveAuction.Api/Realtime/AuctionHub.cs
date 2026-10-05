using LiveAuction.Application.Auctions;
using LiveAuction.Application.Errors;
using LiveAuction.Application.Notifications;
using LiveAuction.Application.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LiveAuction.Api.Realtime;

[Authorize]
public sealed class AuctionHub(GetAuctionSnapshotHandler snapshots, AuctionMetrics metrics) : Hub<IAuctionClient>
{
    public const string Route = "/hubs/auctions";
    public const string WatchMethod = "Watch";
    public const string UnwatchMethod = "Unwatch";

    // Joins the group before reading the snapshot, so an update committed in between is delivered
    // rather than lost. The client keeps whichever state has the higher bid count.
    [HubMethodName(WatchMethod)]
    public async Task<AuctionUpdate> WatchAsync(Guid auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Auction(auctionId), Context.ConnectionAborted);
        try
        {
            return await snapshots.HandleAsync(auctionId, Context.ConnectionAborted);
        }
        catch (NotFoundException notFound)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Auction(auctionId));
            throw new HubException(notFound.Message);
        }
    }

    [HubMethodName(UnwatchMethod)]
    public Task UnwatchAsync(Guid auctionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Auction(auctionId), Context.ConnectionAborted);

    public override Task OnConnectedAsync()
    {
        metrics.WatcherConnected();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        metrics.WatcherDisconnected();
        return base.OnDisconnectedAsync(exception);
    }
}
