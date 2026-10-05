using LiveAuction.Api.Security;
using Microsoft.AspNetCore.SignalR;

namespace LiveAuction.Api.Realtime;

// SignalR looks for ClaimTypes.NameIdentifier by default; our tokens carry the user id in "sub".
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst(AuctionClaimTypes.UserId)?.Value;
}
