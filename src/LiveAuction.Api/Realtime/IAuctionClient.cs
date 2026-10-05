using LiveAuction.Application.Notifications;

namespace LiveAuction.Api.Realtime;

// Method names are the message names browsers subscribe to, so they carry no Async suffix.
public interface IAuctionClient
{
    Task AuctionUpdated(AuctionUpdate update);

    Task Outbid(OutbidNotice notice);
}
