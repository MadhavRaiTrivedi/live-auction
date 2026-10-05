using LiveAuction.Domain.Bidding;

namespace LiveAuction.Domain.Auctions;

public sealed class Auction
{
    private static readonly Dictionary<AuctionStatus, AuctionStatus[]> AllowedTransitions = new()
    {
        [AuctionStatus.Scheduled] = [AuctionStatus.Live, AuctionStatus.Cancelled],
        [AuctionStatus.Live] = [AuctionStatus.Sold, AuctionStatus.Unsold, AuctionStatus.Cancelled],
        [AuctionStatus.Sold] = [],
        [AuctionStatus.Unsold] = [],
        [AuctionStatus.Cancelled] = [],
    };

    private Auction()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public long StartingPriceInPaise { get; private set; }

    public long? ReservePriceInPaise { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public AuctionStatus Status { get; private set; }

    public long? CurrentPriceInPaise { get; private set; }

    public Guid? LeadingBidderId { get; private set; }

    // The most the leader is willing to pay. Equal to the current price unless the leader placed a proxy bid.
    public long? LeaderMaxInPaise { get; private set; }

    public int BidCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public uint Version { get; private set; }

    public bool HasBids => BidCount > 0;

    public bool IsReserveMet => CurrentPriceInPaise is { } price && price >= (ReservePriceInPaise ?? 0);

    public Guid? WinnerId => Status == AuctionStatus.Sold ? LeadingBidderId : null;

    public static Auction Create(Guid sellerId, AuctionTerms terms, DateTimeOffset now)
    {
        var auction = new Auction
        {
            Id = Guid.CreateVersion7(now),
            SellerId = sellerId,
            Status = AuctionStatus.Scheduled,
            CreatedAt = now,
        };
        auction.ApplyTerms(terms, now);
        return auction;
    }

    public void Revise(AuctionTerms terms, DateTimeOffset now)
    {
        if (Status != AuctionStatus.Scheduled)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AuctionNotEditable,
                $"Auction {Id} is {Status} and can no longer be edited.");
        }

        ApplyTerms(terms, now);
    }

    public long MinimumNextBidInPaise(BidIncrementTable increments) =>
        CurrentPriceInPaise is { } price ? price + increments.IncrementFor(price) : StartingPriceInPaise;

    public BidOutcome PlaceManualBid(Guid bidderId, long amountInPaise, BiddingRules rules, DateTimeOffset now) =>
        ResolveOffer(bidderId, BidKind.Manual, amountInPaise, rules, now);

    public BidOutcome PlaceProxyBid(Guid bidderId, long maxAmountInPaise, BiddingRules rules, DateTimeOffset now) =>
        ResolveOffer(bidderId, BidKind.Proxy, maxAmountInPaise, rules, now);

    public void Start(DateTimeOffset now)
    {
        if (now < StartsAt)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AuctionNotStarted,
                $"Auction {Id} starts at {StartsAt:O}.");
        }

        TransitionTo(AuctionStatus.Live);
    }

    public void Close(DateTimeOffset now)
    {
        if (now < EndsAt)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AuctionNotEnded,
                $"Auction {Id} ends at {EndsAt:O}.");
        }

        TransitionTo(IsReserveMet ? AuctionStatus.Sold : AuctionStatus.Unsold);
        ClosedAt = now;
    }

    public void CancelBySeller(DateTimeOffset now)
    {
        if (Status == AuctionStatus.Live && HasBids)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AuctionHasBids,
                $"Auction {Id} already has bids. Only an admin can cancel it now.");
        }

        CancelByAdmin(now);
    }

    public void CancelByAdmin(DateTimeOffset now)
    {
        TransitionTo(AuctionStatus.Cancelled);
        ClosedAt = now;
    }

    private void ApplyTerms(AuctionTerms terms, DateTimeOffset now)
    {
        if (terms.StartingPriceInPaise <= 0 || terms.ReservePriceInPaise < terms.StartingPriceInPaise)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidPrice,
                "The starting price must be positive and the reserve price cannot be below it.");
        }

        if (terms.EndsAt <= terms.StartsAt || terms.EndsAt <= now)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidAuctionSchedule,
                "The auction must end after it starts, and in the future.");
        }

        Title = terms.Title;
        Description = terms.Description;
        StartingPriceInPaise = terms.StartingPriceInPaise;
        ReservePriceInPaise = terms.ReservePriceInPaise;
        StartsAt = terms.StartsAt;
        EndsAt = terms.EndsAt;
    }

    private BidOutcome ResolveOffer(Guid bidderId, BidKind kind, long offerInPaise, BiddingRules rules, DateTimeOffset now)
    {
        EnsureAcceptingBidsFrom(bidderId, now);

        var minimumInPaise = MinimumNextBidInPaise(rules.Increments);
        if (offerInPaise < minimumInPaise)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.BidTooLow,
                $"The minimum bid is {minimumInPaise} paise, got {offerInPaise}.");
        }

        if (LeadingBidderId == bidderId)
        {
            RaiseLeaderMax(kind, offerInPaise);
            return new BidOutcome([], IsBidderLeading: true, OutbidBidderId: null);
        }

        var previousLeaderId = LeadingBidderId;
        var placedBids = previousLeaderId is { } leaderId
            ? Challenge(leaderId, bidderId, kind, offerInPaise, rules.Increments, now)
            : [Record(bidderId, kind == BidKind.Proxy ? StartingPriceInPaise : offerInPaise, kind, now)];

        if (LeadingBidderId == bidderId)
        {
            LeaderMaxInPaise = offerInPaise;
        }

        ExtendIfClosing(rules, now);
        var outbidBidderId = LeadingBidderId == bidderId ? previousLeaderId : null;
        return new BidOutcome(placedBids, LeadingBidderId == bidderId, outbidBidderId);
    }

    private List<Bid> Challenge(
        Guid leaderId,
        Guid challengerId,
        BidKind kind,
        long offerInPaise,
        BidIncrementTable increments,
        DateTimeOffset now)
    {
        var leaderMaxInPaise = LeaderMaxInPaise!.Value;
        if (offerInPaise <= leaderMaxInPaise)
        {
            var challengerBid = Record(challengerId, offerInPaise, kind, now);
            var defenceInPaise = Math.Min(leaderMaxInPaise, offerInPaise + increments.IncrementFor(offerInPaise));
            return [challengerBid, Record(leaderId, defenceInPaise, BidKind.Auto, now)];
        }

        var placedBids = new List<Bid>();
        if (leaderMaxInPaise > CurrentPriceInPaise)
        {
            placedBids.Add(Record(leaderId, leaderMaxInPaise, BidKind.Auto, now));
        }

        var amountInPaise = kind == BidKind.Proxy
            ? Math.Min(offerInPaise, leaderMaxInPaise + increments.IncrementFor(leaderMaxInPaise))
            : offerInPaise;
        placedBids.Add(Record(challengerId, amountInPaise, kind, now));
        return placedBids;
    }

    private void RaiseLeaderMax(BidKind kind, long offerInPaise)
    {
        if (kind != BidKind.Proxy || offerInPaise <= LeaderMaxInPaise)
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.AlreadyLeading,
                "You are already the leading bidder. Only a higher maximum bid can be set.");
        }

        LeaderMaxInPaise = offerInPaise;
    }

    private Bid Record(Guid bidderId, long amountInPaise, BidKind kind, DateTimeOffset now)
    {
        BidCount++;
        CurrentPriceInPaise = amountInPaise;
        LeadingBidderId = bidderId;
        LeaderMaxInPaise = Math.Max(LeaderMaxInPaise ?? 0, amountInPaise);
        return Bid.Create(Id, BidCount, bidderId, amountInPaise, kind, now);
    }

    private void EnsureAcceptingBidsFrom(Guid bidderId, DateTimeOffset now)
    {
        if (Status != AuctionStatus.Live)
        {
            throw new DomainRuleViolationException(DomainErrorCode.AuctionNotLive, $"Auction {Id} is {Status}.");
        }

        if (now >= EndsAt)
        {
            throw new DomainRuleViolationException(DomainErrorCode.AuctionEnded, $"Auction {Id} ended at {EndsAt:O}.");
        }

        if (bidderId == SellerId)
        {
            throw new DomainRuleViolationException(DomainErrorCode.SellerCannotBid, "Sellers cannot bid on their own auction.");
        }
    }

    private void ExtendIfClosing(BiddingRules rules, DateTimeOffset now)
    {
        if (now >= EndsAt - rules.SoftCloseWindow)
        {
            var extendedEnd = now + rules.SoftCloseExtension;
            EndsAt = extendedEnd > EndsAt ? extendedEnd : EndsAt;
        }
    }

    private void TransitionTo(AuctionStatus newStatus)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainRuleViolationException(
                DomainErrorCode.InvalidStatusTransition,
                $"Auction {Id} cannot move from {Status} to {newStatus}.");
        }

        Status = newStatus;
    }
}
