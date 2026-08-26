using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Error = ItemAuthoring.Application.Common.Error;

namespace ItemAuthoring.Api.Common;

/// <summary>
/// Converts an unhandled exception into an RFC 9457 problem document.
/// </summary>
/// <remarks>
/// Only genuinely unexpected failures reach this handler: expected outcomes are returned as
/// <see cref="Result"/> values and translated by the controllers. The message of an unexpected
/// exception is never returned to the caller, because it frequently contains connection strings,
/// SQL fragments or file paths.
/// </remarks>
/// <param name="logger">The logger.</param>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = httpContext.GetCorrelationId();
        var error = Translate(exception);

        if (error.Type is ErrorType.Failure)
        {
            logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path} ({CorrelationId}).",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }

        var problem = ApiProblemDetailsFactory.Create(error, httpContext.Request.Path, correlationId);
        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static Error Translate(Exception exception) => exception switch
    {
        DomainException domainException
            => Error.Conflict(domainException.Code, domainException.Message),
        BadHttpRequestException
            => Error.Validation("request.malformed", "The request body could not be read."),
        _ => Error.Failure(
            "server.unexpected",
            "An unexpected error occurred. Quote the correlation identifier when reporting it."),
    };
}
