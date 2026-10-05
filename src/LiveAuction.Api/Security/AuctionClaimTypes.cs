using Microsoft.IdentityModel.JsonWebTokens;

namespace LiveAuction.Api.Security;

public static class AuctionClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;
    public const string Role = "role";
}
