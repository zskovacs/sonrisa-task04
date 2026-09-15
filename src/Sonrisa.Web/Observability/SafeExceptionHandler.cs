using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace Sonrisa.Web.Observability;

internal sealed class SafeExceptionHandler(ILogger<SafeExceptionHandler> logger) : IExceptionHandler
{
    private static readonly EventId UnexpectedRequestFailure = new(1000, nameof(UnexpectedRequestFailure));

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceCorrelation = Activity.Current is { } activity
            ? activity.TraceId.ToString()
            : ActivityTraceId.CreateRandom().ToString();

        logger.LogError(UnexpectedRequestFailure,
            "Unexpected request failure. Exception type {ExceptionType}. Trace correlation {TraceCorrelation}.",
            exception.GetType().Name, traceCorrelation);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "text/plain; charset=utf-8";
        await httpContext.Response.WriteAsync("An unexpected error occurred.", cancellationToken);
        return true;
    }
}
