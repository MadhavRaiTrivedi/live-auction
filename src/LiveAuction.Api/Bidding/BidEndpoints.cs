using LiveAuction.Api.Http;
using LiveAuction.Application.Bidding;
using LiveAuction.Domain.Bidding;

namespace LiveAuction.Api.Bidding;

internal static class BidEndpoints
{
    public static void MapBidEndpoints(this IEndpointRouteBuilder app)
    {
        var bids = app.MapGroup("/api/auctions/{auctionId:guid}").WithTags("Bidding").RequireAuthorization();

        bids.MapGet("/bids", ListAsync);
        bids.MapPost("/bids", PlaceAsync).RequireRateLimiting(RateLimitingExtensions.BidsPolicy);
        bids.MapPost("/proxy-bids", PlaceProxyAsync).RequireRateLimiting(RateLimitingExtensions.BidsPolicy);
    }

    private static Task<IReadOnlyList<BidResponse>> ListAsync(
        Guid auctionId,
        GetBidHistoryHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(auctionId, cancellationToken);

    private static Task<PlaceBidResponse> PlaceAsync(
        Guid auctionId,
        PlaceBidRequest request,
        PlaceBidHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new PlaceBidCommand(auctionId, request.AmountInPaise, BidKind.Manual), cancellationToken);

    private static Task<PlaceBidResponse> PlaceProxyAsync(
        Guid auctionId,
        PlaceProxyBidRequest request,
        PlaceBidHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new PlaceBidCommand(auctionId, request.MaxAmountInPaise, BidKind.Proxy), cancellationToken);
}
