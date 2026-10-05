using LiveAuction.Application.Errors;
using LiveAuction.Domain.Auctions;

namespace LiveAuction.Application.Security;

public sealed record Requester(Guid UserId, bool IsAdmin)
{
    public bool IsSellerOf(Auction auction) => auction.SellerId == UserId;

    public bool CanSeeReserveOf(Auction auction) => IsAdmin || IsSellerOf(auction);

    public void EnsureIsSellerOf(Auction auction)
    {
        if (!IsSellerOf(auction))
        {
            throw new AccessDeniedException($"Only the seller can change auction {auction.Id}.");
        }
    }
}
