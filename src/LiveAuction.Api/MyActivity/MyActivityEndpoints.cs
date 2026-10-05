using LiveAuction.Application.Auctions;
using LiveAuction.Application.Bidding;

namespace LiveAuction.Api.MyActivity;

internal static class MyActivityEndpoints
{
    public static void MapMyActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").WithTags("My activity").RequireAuthorization();

        me.MapGet("/auctions", (ListSellerAuctionsHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(cancellationToken));
        me.MapGet("/bids", (ListBidderAuctionsHandler handler, CancellationToken cancellationToken) =>
            handler.HandleAsync(cancellationToken));
    }
}
