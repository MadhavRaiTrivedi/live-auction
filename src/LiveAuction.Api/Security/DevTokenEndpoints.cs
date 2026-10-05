using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LiveAuction.Api.Security;

internal static class DevTokenEndpoints
{
    // Stands in for a real identity provider so the app can be tried locally. Disabled unless configured.
    public static void MapDevTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/dev-token", IssueToken)
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private static DevTokenResponse IssueToken(DevTokenRequest request, IOptions<JwtOptions> options, TimeProvider clock)
    {
        var jwt = options.Value;
        var userId = request.UserId ?? Guid.NewGuid();
        var expiresAt = clock.GetUtcNow() + jwt.TokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(AuctionClaimTypes.UserId, userId.ToString()),
                new Claim(AuctionClaimTypes.Role, request.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new DevTokenResponse(token, userId, request.Role, expiresAt);
    }
}
