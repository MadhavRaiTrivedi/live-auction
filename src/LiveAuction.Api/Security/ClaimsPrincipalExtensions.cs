using System.Security.Claims;
using LiveAuction.Application.Errors;
using LiveAuction.Application.Security;

namespace LiveAuction.Api.Security;

internal static class ClaimsPrincipalExtensions
{
    public static Requester ToRequester(this ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirstValue(AuctionClaimTypes.UserId), out var userId))
        {
            throw new AccessDeniedException("The access token has no valid subject.");
        }

        return new Requester(userId, user.IsInRole(nameof(UserRole.Admin)));
    }
}
