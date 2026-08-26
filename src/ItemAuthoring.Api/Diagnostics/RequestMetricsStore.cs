using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ItemAuthoring.Api.Diagnostics;

/// <summary>A single observed request, tagged with the transport that served it.</summary>
/// <param name="TimestampUtc">The instant the request completed.</param>
/// <param name="Transport">The API surface that served the request, <c>rest</c> or <c>graphql</c>.</param>
/// <param name="Operation">The logical operation, for example <c>items.search</c>.</param>
/// <param name="StatusCode">The HTTP status code returned.</param>
/// <param name="DurationMilliseconds">The server side wall-clock duration.</param>
/// <param name="ResponseBytes">The size of the serialized response body, before compression.</param>
/// <param name="DatabaseCommands">The number of database round trips the request caused.</param>
/// <param name="CorrelationId">The correlation identifier of the request.</param>
public sealed record RequestMeasurement(
    DateTimeOffset TimestampUtc,
    string Transport,
    string Operation,
    int StatusCode,
    double DurationMilliseconds,
    long ResponseBytes,
    int DatabaseCommands,
    string? CorrelationId);

/// <summary>
/// An in-process ring buffer of request measurements.
/// </summary>
/// <remarks>
/// <para>
/// The store is intentionally bounded and in-memory. Its purpose is to support a controlled benchmark
/// run against a known data set, not to be a production telemetry pipeline — that role belongs to the
/// OpenTelemetry exporter, and duplicating it here would be a worse version of a solved problem.
/// </para>
/// <para>
/// Measurements are taken at the outermost point of the server pipeline, identically for both
/// surfaces, so the difference between two samples is attributable to the transport and not to where
/// the stopwatch was started.
/// </para>
/// </remarks>
public sealed class RequestMetricsStore
{
    /// <summary>The maximum number of measurements retained.</summary>
    public const int Capacity = 20_000;

    private readonly ConcurrentQueue<RequestMeasurement> _measurements = new();

    /// <summary>Records a measurement, discarding the oldest one when the buffer is full.</summary>
    /// <param name="measurement">The measurement to record.</param>
    public void Record(RequestMeasurement measurement)
    {
        _measurements.Enqueue(measurement);
        while (_measurements.Count > Capacity && _measurements.TryDequeue(out _))
        {
            // Discarding the oldest entry is the intended behaviour of a bounded buffer.
        }
    }

    /// <summary>Reads every retained measurement, oldest first.</summary>
    /// <returns>The retained measurements.</returns>
    public IReadOnlyList<RequestMeasurement> Snapshot() => [.. _measurements];

    /// <summary>Discards every retained measurement, so a benchmark run can start from a clean slate.</summary>
    public void Clear()
    {
        while (_measurements.TryDequeue(out _))
        {
            // Draining the queue is the intended behaviour.
        }
    }

    /// <summary>Renders the retained measurements as CSV for offline analysis.</summary>
    /// <returns>The CSV document.</returns>
    public string ToCsv()
    {
        var builder = new StringBuilder(
            "timestampUtc,transport,operation,statusCode,durationMs,responseBytes,databaseCommands,correlationId\n");

        foreach (var measurement in Snapshot())
        {
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.TimestampUtc:O},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.Transport},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.Operation},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.StatusCode},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.DurationMilliseconds:F3},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.ResponseBytes},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.DatabaseCommands},");
            builder.Append(CultureInfo.InvariantCulture, $"{measurement.CorrelationId}\n");
        }

        return builder.ToString();
    }
}
