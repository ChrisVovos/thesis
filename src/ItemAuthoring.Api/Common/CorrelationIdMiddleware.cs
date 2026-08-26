namespace ItemAuthoring.Api.Common;

/// <summary>
/// Assigns a correlation identifier to every request and echoes it back to the caller.
/// </summary>
/// <remarks>
/// The same middleware runs in front of both API surfaces, so a REST call and a GraphQL call made by
/// the same client session can be joined in the logs, and a benchmark run can be reconstructed from
/// the server side alone.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The request and response header carrying the correlation identifier.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The <c>HttpContext.Items</c> key under which the identifier is published.</summary>
    public const string ItemKey = "CorrelationId";

    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="logger">The logger used to open the logging scope.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.CreateVersion7().ToString("N");
        }

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using var scope = logger.BeginScope(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["CorrelationId"] = correlationId,
        });

        await next(context);
    }
}

/// <summary>Reads the correlation identifier assigned to the current request.</summary>
public static class CorrelationIdAccessor
{
    /// <summary>Reads the correlation identifier of the request, when one has been assigned.</summary>
    /// <param name="context">The current request context.</param>
    /// <returns>The correlation identifier, or <see langword="null"/>.</returns>
    public static string? GetCorrelationId(this HttpContext? context)
        => context?.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) is true
            ? value as string
            : null;
}
