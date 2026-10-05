namespace LiveAuction.Application.Notifications;

// Called after the change is committed. Delivery is best effort: a watcher that misses an update
// gets the current state when it reconnects and watches the auction again.
public interface IAuctionNotifier
{
    Task AuctionChangedAsync(AuctionUpdate update, CancellationToken cancellationToken);

    Task OutbidAsync(Guid bidderId, OutbidNotice notice, CancellationToken cancellationToken);
}
