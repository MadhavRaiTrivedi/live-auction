namespace LiveAuction.Api.Realtime;

internal static class HubGroups
{
    public static string Auction(Guid auctionId) => $"auction:{auctionId}";
}
