namespace LiveAuction.Application.Auctions;

public sealed class ListAuctionsHandler(IAuctionReader reader)
{
    public const int PageSize = 20;

    public async Task<AuctionPage> HandleAsync(ListAuctionsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var auctions = await reader.SearchAsync(query, (page - 1) * PageSize, PageSize + 1, cancellationToken);
        return new AuctionPage([.. auctions.Take(PageSize).Select(AuctionSummary.From)], page, auctions.Count > PageSize);
    }
}
