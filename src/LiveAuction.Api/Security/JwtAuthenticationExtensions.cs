using System.Text;
using LiveAuction.Api.Realtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LiveAuction.Api.Security;

internal static class JwtAuthenticationExtensions
{
    private const string AccessTokenQueryParameter = "access_token";
    private const int MinSigningKeyBytes = 32;

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.SigningKey) >= MinSigningKeyBytes,
                $"Jwt:SigningKey must be at least {MinSigningKeyBytes} bytes.")
            .ValidateOnStart();
        var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    NameClaimType = AuctionClaimTypes.UserId,
                    RoleClaimType = AuctionClaimTypes.Role,
                };

                // Browsers cannot set headers on a WebSocket handshake, so the hub token arrives in the query string.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Path.StartsWithSegments(AuctionHub.Route))
                        {
                            context.Token = context.Request.Query[AccessTokenQueryParameter];
                        }

                        return Task.CompletedTask;
                    },
                };
            });
        services.AddAuthorization();

        return services;
    }
}
