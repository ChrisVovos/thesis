using System.Text.Json;
using Asp.Versioning;
using ItemAuthoring.Api.Diagnostics;
using ItemAuthoring.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// The benchmark harness endpoint used by the comparative study.
/// </summary>
/// <remarks>
/// The endpoint is registered only outside production, so the experiment tooling cannot leak into a
/// real deployment. It is still authenticated, because the measurements reveal the shape of internal
/// traffic.
/// </remarks>
/// <param name="sender">The request dispatcher.</param>
/// <param name="store">The measurement store.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/benchmark")]
[Authorize]
public sealed class BenchmarkController(ISender sender, RequestMetricsStore store)
    : ApiControllerBase(sender)
{
    /// <summary>Reads every retained measurement as JSON.</summary>
    /// <returns>The retained measurements.</returns>
    [HttpGet("measurements", Name = nameof(GetMeasurements))]
    [ProducesResponseType(typeof(IReadOnlyList<RequestMeasurement>), StatusCodes.Status200OK)]
    public IActionResult GetMeasurements() => Ok(store.Snapshot());

    /// <summary>Reads every retained measurement as CSV.</summary>
    /// <returns>The retained measurements as a downloadable CSV file.</returns>
    [HttpGet("measurements.csv", Name = nameof(GetMeasurementsCsv))]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetMeasurementsCsv()
        => File(System.Text.Encoding.UTF8.GetBytes(store.ToCsv()), "text/csv", "measurements.csv");

    /// <summary>Summarises the retained measurements per transport and operation.</summary>
    /// <returns>The aggregated statistics.</returns>
    [HttpGet("summary", Name = nameof(GetSummary))]
    [ProducesResponseType(typeof(IReadOnlyList<BenchmarkSummaryRow>), StatusCodes.Status200OK)]
    public IActionResult GetSummary()
    {
        var summary = store.Snapshot()
            .GroupBy(measurement => (measurement.Transport, measurement.Operation))
            .Select(group => new BenchmarkSummaryRow(
                group.Key.Transport,
                group.Key.Operation,
                group.Count(),
                Percentile(group.Select(measurement => measurement.DurationMilliseconds), 0.50),
                Percentile(group.Select(measurement => measurement.DurationMilliseconds), 0.95),
                group.Average(measurement => measurement.ResponseBytes),
                group.Average(measurement => (double)measurement.DatabaseCommands)))
            .OrderBy(row => row.Operation, StringComparer.Ordinal)
            .ThenBy(row => row.Transport, StringComparer.Ordinal)
            .ToList();

        return Ok(summary);
    }

    /// <summary>Discards every retained measurement so a run can start from a clean slate.</summary>
    /// <returns>No content.</returns>
    [HttpDelete("measurements", Name = nameof(ClearMeasurements))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearMeasurements()
    {
        store.Clear();
        return NoContent();
    }

    private static double Percentile(IEnumerable<double> values, double percentile)
    {
        var ordered = values.Order().ToList();
        if (ordered.Count == 0)
        {
            return 0d;
        }

        var index = (int)Math.Ceiling(percentile * ordered.Count) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Count - 1)];
    }
}

/// <summary>One aggregated row of the benchmark summary.</summary>
/// <param name="Transport">The API surface, <c>rest</c> or <c>graphql</c>.</param>
/// <param name="Operation">The logical operation.</param>
/// <param name="Samples">The number of observations.</param>
/// <param name="MedianDurationMs">The median server side duration.</param>
/// <param name="P95DurationMs">The 95th percentile server side duration.</param>
/// <param name="MeanResponseBytes">The mean uncompressed response size.</param>
/// <param name="MeanDatabaseCommands">The mean number of database round trips.</param>
public sealed record BenchmarkSummaryRow(
    string Transport,
    string Operation,
    int Samples,
    double MedianDurationMs,
    double P95DurationMs,
    double MeanResponseBytes,
    double MeanDatabaseCommands);
