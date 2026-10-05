using System.Diagnostics;

namespace LiveAuction.Api.Http;

internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadOrCreate(context);
        context.Response.Headers[AuctionHeaders.CorrelationId] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ReadOrCreate(HttpContext context)
    {
        var supplied = context.Request.Headers[AuctionHeaders.CorrelationId].ToString();
        if (!string.IsNullOrWhiteSpace(supplied) && supplied.Length <= MaxCorrelationIdLength)
        {
            return supplied;
        }

        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
