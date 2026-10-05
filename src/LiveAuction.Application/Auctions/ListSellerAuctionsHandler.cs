using LiveAuction.Application.Security;

namespace LiveAuction.Application.Auctions;

public sealed class ListSellerAuctionsHandler(IAuctionReader reader, IRequestContext requestContext)
{
    private const int MaxResults = 100;

    public async Task<IReadOnlyList<SellerAuctionResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var auctions = await reader.ListBySellerAsync(requestContext.Requester.UserId, MaxResults, cancellationToken);
        return [.. auctions.Select(SellerAuctionResponse.From)];
    }
}
