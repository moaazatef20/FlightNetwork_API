using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FlightNetwork.Api.ErrorHandling;

/// <summary>
/// Last-resort translation of unhandled exceptions into ProblemDetails responses. Services and
/// DataAccess throw ArgumentException (and its Null/OutOfRange subtypes) for invalid caller
/// input — e.g. a maxHops outside RouteRepository.MaxSupportedHops, a blank airport code, a
/// page/pageSize outside Paging's bounds — so those map to 400. Anything else (a dropped Neo4j
/// connection, a driver timeout, a bug) is an infrastructure or programming failure the caller
/// didn't cause: it's logged with full detail here and returned as an opaque 500, since leaking
/// driver/internal exception text to a client is an information disclosure risk.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status400BadRequest ? exception.Message : null,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
