using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Bidding;

public sealed record BidderAuctionResponse(
    Guid AuctionId,
    string Title,
    AuctionStatus Status,
    long? CurrentPriceInPaise,
    long YourHighestBidInPaise,
    DateTimeOffset EndsAt,
    BidStanding Standing)
{
    public static BidderAuctionResponse From(BidderAuction bidderAuction, Guid bidderId)
    {
        var auction = bidderAuction.Auction;
        var isLeader = auction.LeadingBidderId == bidderId;
        var standing = auction.Status switch
        {
            AuctionStatus.Sold => auction.WinnerId == bidderId ? BidStanding.Won : BidStanding.Lost,
            AuctionStatus.Unsold => BidStanding.Lost,
            AuctionStatus.Cancelled => BidStanding.Cancelled,
            _ => isLeader ? BidStanding.Leading : BidStanding.Outbid,
        };

        return new BidderAuctionResponse(
            auction.Id,
            auction.Title,
            auction.Status,
            auction.CurrentPriceInPaise,
            bidderAuction.HighestBidInPaise,
            auction.EndsAt,
            standing);
    }
}
