using LiveAuction.Application.Auctions;
using LiveAuction.Domain.Auctions;

namespace LiveAuction.Api.Auctions;

internal static class AuctionEndpoints
{
    public static void MapAuctionEndpoints(this IEndpointRouteBuilder app)
    {
        var auctions = app.MapGroup("/api/auctions").WithTags("Auctions").RequireAuthorization();

        auctions.MapGet("/", ListAsync);
        auctions.MapGet("/{auctionId:guid}", GetAsync);
        auctions.MapPost("/", CreateAsync);
        auctions.MapPut("/{auctionId:guid}", ReviseAsync);
        auctions.MapPost("/{auctionId:guid}/cancel", CancelAsync);
    }

    private static Task<AuctionPage> ListAsync(
        string? search,
        AuctionStatus? status,
        int? page,
        ListAuctionsHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ListAuctionsQuery(search, status, page ?? 1), cancellationToken);

    private static Task<AuctionResponse> GetAsync(
        Guid auctionId,
        GetAuctionHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(auctionId, cancellationToken);

    private static async Task<IResult> CreateAsync(
        AuctionTermsRequest request,
        CreateAuctionHandler handler,
        CancellationToken cancellationToken)
    {
        var auction = await handler.HandleAsync(request.ToTerms(), cancellationToken);
        return TypedResults.Created($"/api/auctions/{auction.Id}", auction);
    }

    private static Task<AuctionResponse> ReviseAsync(
        Guid auctionId,
        AuctionTermsRequest request,
        ReviseAuctionHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new ReviseAuctionCommand(auctionId, request.ToTerms()), cancellationToken);

    private static Task<AuctionResponse> CancelAsync(
        Guid auctionId,
        CancelAuctionHandler handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(auctionId, cancellationToken);
}
