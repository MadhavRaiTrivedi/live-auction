namespace LiveAuction.Domain;

public enum DomainErrorCode
{
    InvalidAuctionSchedule,
    InvalidPrice,
    InvalidStatusTransition,
    AuctionNotEditable,
    AuctionNotStarted,
    AuctionNotEnded,
    AuctionHasBids,
    AuctionNotLive,
    AuctionEnded,
    SellerCannotBid,
    BidTooLow,
    AlreadyLeading,
}
