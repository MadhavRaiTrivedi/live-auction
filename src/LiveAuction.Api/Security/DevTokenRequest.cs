namespace LiveAuction.Api.Security;

public sealed record DevTokenRequest(Guid? UserId, UserRole Role);
