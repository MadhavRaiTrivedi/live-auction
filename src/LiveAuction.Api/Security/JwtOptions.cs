namespace LiveAuction.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "live-auction";

    public string Audience { get; init; } = "live-auction-api";

    public string SigningKey { get; init; } = string.Empty;

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(8);

    public bool EnableDevTokenEndpoint { get; init; }
}
