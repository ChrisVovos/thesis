using System.Diagnostics;
using ItemAuthoring.Api.Common;
using ItemAuthoring.Application.Abstractions.Diagnostics;

namespace ItemAuthoring.Api.Diagnostics;

/// <summary>
/// Measures every request that reaches either API surface.
/// </summary>
/// <remarks>
/// The middleware sits outside authentication and routing so that the timing includes everything the
/// client actually waits for. Response size is taken from the byte count written to the body rather
/// than from the <c>Content-Length</c> header, because chunked responses do not carry one.
/// </remarks>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="store">The measurement store.</param>
public sealed class RequestMetricsMiddleware(RequestDelegate next, RequestMetricsStore store)
{
    /// <summary>The path prefix that identifies the GraphQL surface.</summary>
    public const string GraphQlPath = "/graphql";

    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="commandCounter">The database command counter for this request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IDatabaseCommandCounter commandCounter)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(commandCounter);

        var originalBody = context.Response.Body;
        await using var counting = new ByteCountingStream(originalBody);
        context.Response.Body = counting;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            context.Response.Body = originalBody;

            store.Record(new RequestMeasurement(
                DateTimeOffset.UtcNow,
                ResolveTransport(context),
                ResolveOperation(context),
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                counting.BytesWritten,
                commandCounter.Count,
                context.GetCorrelationId()));
        }
    }

    private static string ResolveTransport(HttpContext context)
        => context.Request.Path.StartsWithSegments(GraphQlPath, StringComparison.OrdinalIgnoreCase)
            ? "graphql"
            : "rest";

    private static string ResolveOperation(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(OperationHeaderName, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return declared.ToString();
        }

        var endpoint = context.GetEndpoint()?.DisplayName;
        return endpoint ?? $"{context.Request.Method} {context.Request.Path}";
    }

    /// <summary>
    /// The header a client sets to name the logical operation it is performing.
    /// </summary>
    /// <remarks>
    /// A GraphQL request always arrives as <c>POST /graphql</c>, so the server cannot tell "load the
    /// item list" from "load one item" by inspecting the route. The client declares the operation
    /// name, which is what makes a like-for-like comparison with the REST route possible.
    /// </remarks>
    public const string OperationHeaderName = "X-Benchmark-Operation";

    private sealed class ByteCountingStream(Stream inner) : Stream
    {
        public long BytesWritten { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            BytesWritten += count;
            inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            BytesWritten += buffer.Length;
            inner.Write(buffer);
        }

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            BytesWritten += count;
            return inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            BytesWritten += buffer.Length;
            return inner.WriteAsync(buffer, cancellationToken);
        }
    }
}
