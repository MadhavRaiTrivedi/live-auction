using LiveAuction.Application.Security;

namespace LiveAuction.Api.Security;

internal sealed class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public Requester Requester =>
        (httpContextAccessor.HttpContext ?? throw new InvalidOperationException("There is no active HTTP request."))
            .User.ToRequester();
}
