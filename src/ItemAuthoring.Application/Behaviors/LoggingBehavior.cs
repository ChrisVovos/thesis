using System.Diagnostics;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using Microsoft.Extensions.Logging;

namespace ItemAuthoring.Application.Behaviors;

/// <summary>
/// Records the start, outcome and duration of every request.
/// </summary>
/// <remarks>
/// The measurement is taken here, around the application layer, rather than in each API surface.
/// That is deliberate: the study needs to attribute latency either to business logic or to the
/// transport, and it can only do so if the same inner span is timed identically for REST and for
/// GraphQL.
/// </remarks>
/// <typeparam name="TRequest">The request type being executed.</typeparam>
/// <typeparam name="TResponse">The response type of the pipeline.</typeparam>
/// <param name="logger">The logger for the request type.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Requests slower than this are logged as a warning.</summary>
    public const int SlowRequestThresholdMilliseconds = 500;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RequestName"] = requestName,
            ["UserId"] = currentUser.UserId?.Value,
        });

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            LogOutcome(requestName, response, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "Request {RequestName} threw after {ElapsedMilliseconds} ms.",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogOutcome(string requestName, TResponse response, long elapsedMilliseconds)
    {
        if (response is Result { IsFailure: true } failure)
        {
            logger.LogWarning(
                "Request {RequestName} failed with {ErrorCode} ({ErrorType}) after {ElapsedMilliseconds} ms.",
                requestName,
                failure.Error.Code,
                failure.Error.Type,
                elapsedMilliseconds);
            return;
        }

        if (elapsedMilliseconds >= SlowRequestThresholdMilliseconds)
        {
            logger.LogWarning(
                "Request {RequestName} completed slowly in {ElapsedMilliseconds} ms.",
                requestName,
                elapsedMilliseconds);
            return;
        }

        logger.LogInformation(
            "Request {RequestName} completed in {ElapsedMilliseconds} ms.",
            requestName,
            elapsedMilliseconds);
    }
}
