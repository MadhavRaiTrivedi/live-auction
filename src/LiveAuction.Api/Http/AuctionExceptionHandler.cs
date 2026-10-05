using LiveAuction.Application.Errors;
using LiveAuction.Application.Persistence;
using LiveAuction.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LiveAuction.Api.Http;

internal sealed class AuctionExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string ErrorCodeExtensionKey = "code";

    private static readonly HashSet<DomainErrorCode> ConflictCodes =
    [
        DomainErrorCode.InvalidStatusTransition,
        DomainErrorCode.AuctionNotEditable,
        DomainErrorCode.AuctionNotLive,
        DomainErrorCode.AuctionEnded,
    ];

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = ToProblem(exception);
        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblem(Exception exception) => exception switch
    {
        DomainRuleViolationException violation => new ProblemDetails
        {
            Status = ConflictCodes.Contains(violation.Code)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status422UnprocessableEntity,
            Title = "Auction rule violated",
            Detail = violation.Message,
            Extensions = { [ErrorCodeExtensionKey] = violation.Code.ToString() },
        },
        ConcurrencyConflictException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Too many bids at once",
            Detail = "Other bids kept changing the auction while yours was processed. Check the new price and try again.",
        },
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = $"{notFound.Resource} not found",
            Detail = notFound.Message,
        },
        AccessDeniedException accessDenied => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access denied",
            Detail = accessDenied.Message,
        },
        InvalidRequestException invalidRequest => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request",
            Detail = invalidRequest.Message,
        },
        _ => null,
    };
}
