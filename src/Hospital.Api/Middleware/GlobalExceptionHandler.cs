using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Hospital.Application.Exceptions;

namespace Hospital.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> UnhandledRequestFailure = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(1, nameof(UnhandledRequestFailure)),
        "Unhandled exception while processing {RequestPath}.");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ConflictException)
        {
            return await WriteProblemAsync(httpContext, StatusCodes.Status409Conflict, "Conflict", exception.Message, cancellationToken);
        }

        if (exception is AuthenticationException)
        {
            return await WriteProblemAsync(httpContext, StatusCodes.Status401Unauthorized, "Authentication failed", "Invalid email or password.", cancellationToken);
        }

        if (exception is NotFoundException)
        {
            return await WriteProblemAsync(httpContext, StatusCodes.Status404NotFound, "Not found", exception.Message, cancellationToken);
        }

        UnhandledRequestFailure(logger, httpContext.Request.Path, exception);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected server error occurred.",
            Type = "https://httpstatuses.com/500",
        };

        return await WriteProblemAsync(httpContext, problemDetails, cancellationToken);
    }

    private static async ValueTask<bool> WriteProblemAsync(HttpContext httpContext, int status, string title, string detail, CancellationToken cancellationToken) =>
        await WriteProblemAsync(httpContext, new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
        }, cancellationToken);

    private static async ValueTask<bool> WriteProblemAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
