using System.Diagnostics.Metrics;
using LiveAuction.Domain;
using LiveAuction.Domain.Auctions;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Application.Observability;

public sealed class AuctionMetrics
{
    public const string MeterName = "LiveAuction";

    private const string BidKindTag = "bid.kind";
    private const string RejectionReasonTag = "bid.rejection_reason";
    private const string AuctionStatusTag = "auction.status";

    private readonly Counter<long> _bidsAccepted;
    private readonly Counter<long> _bidsRejected;
    private readonly Counter<long> _bidConflicts;
    private readonly Counter<long> _auctionsClosed;
    private readonly UpDownCounter<long> _connectedWatchers;

    public AuctionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _bidsAccepted = meter.CreateCounter<long>("auction.bids.accepted", description: "Bids recorded, including automatic bids.");
        _bidsRejected = meter.CreateCounter<long>("auction.bids.rejected", description: "Bids rejected by a bidding rule.");
        _bidConflicts = meter.CreateCounter<long>(
            "auction.bids.conflicts", description: "Bid attempts retried because another bid changed the auction first.");
        _auctionsClosed = meter.CreateCounter<long>("auction.closed", description: "Auctions closed by final status.");
        _connectedWatchers = meter.CreateUpDownCounter<long>(
            "auction.watchers.connected", description: "Open real-time connections on this instance.");
    }

    public void BidsAccepted(IEnumerable<Bid> bids)
    {
        foreach (var bid in bids)
        {
            _bidsAccepted.Add(1, new KeyValuePair<string, object?>(BidKindTag, bid.Kind.ToString()));
        }
    }

    public void BidRejected(DomainErrorCode reason) =>
        _bidsRejected.Add(1, new KeyValuePair<string, object?>(RejectionReasonTag, reason.ToString()));

    public void BidConflict() => _bidConflicts.Add(1);

    public void AuctionClosed(AuctionStatus status) =>
        _auctionsClosed.Add(1, new KeyValuePair<string, object?>(AuctionStatusTag, status.ToString()));

    public void WatcherConnected() => _connectedWatchers.Add(1);

    public void WatcherDisconnected() => _connectedWatchers.Add(-1);
}
