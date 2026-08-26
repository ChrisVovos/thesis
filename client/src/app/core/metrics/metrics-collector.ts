import { Injectable, signal } from '@angular/core';
import type { ApiTransport } from '../transport/api-transport';

/** One observed logical operation, tagged with the transport that served it. */
export interface OperationMeasurement {
  readonly timestamp: string;
  readonly transport: ApiTransport;
  readonly operation: string;
  readonly durationMs: number;
  readonly requestCount: number;
  readonly responseBytes: number;
  readonly succeeded: boolean;
}

/**
 * Records what each logical operation cost the client.
 *
 * The measurement is taken at the gateway boundary, which is deliberately where the transport
 * specific work — request construction, response parsing and mapping to the shared view models —
 * begins and ends. Mapping cost therefore lands on the transport that incurs it, which is the honest
 * place for it: a GraphQL response that needs more reshaping should show that in its numbers.
 */
@Injectable({ providedIn: 'root' })
export class MetricsCollector {
  /** The maximum number of measurements retained in memory. */
  static readonly Capacity = 5000;

  private readonly measurements = signal<readonly OperationMeasurement[]>([]);

  /** Every retained measurement, oldest first. */
  readonly samples = this.measurements.asReadonly();

  /**
   * Records one completed operation.
   *
   * @param measurement The measurement to record.
   */
  record(measurement: OperationMeasurement): void {
    this.measurements.update((existing) => {
      const next = [...existing, measurement];
      return next.length > MetricsCollector.Capacity
        ? next.slice(next.length - MetricsCollector.Capacity)
        : next;
    });
  }

  /** Discards every retained measurement so a run can start from a clean slate. */
  clear(): void {
    this.measurements.set([]);
  }

  /**
   * Summarises the retained measurements per transport and operation.
   *
   * @returns One row per transport and operation pair.
   */
  summarize(): readonly MetricsSummaryRow[] {
    const groups = new Map<string, OperationMeasurement[]>();

    for (const measurement of this.measurements()) {
      const key = `${measurement.transport}|${measurement.operation}`;
      const bucket = groups.get(key);
      if (bucket) {
        bucket.push(measurement);
      } else {
        groups.set(key, [measurement]);
      }
    }

    return [...groups.values()]
      .map((bucket) => ({
        transport: bucket[0].transport,
        operation: bucket[0].operation,
        samples: bucket.length,
        medianDurationMs: percentile(bucket.map((entry) => entry.durationMs), 0.5),
        p95DurationMs: percentile(bucket.map((entry) => entry.durationMs), 0.95),
        meanResponseBytes: mean(bucket.map((entry) => entry.responseBytes)),
        meanRequestCount: mean(bucket.map((entry) => entry.requestCount)),
      }))
      .sort((left, right) =>
        left.operation === right.operation
          ? left.transport.localeCompare(right.transport)
          : left.operation.localeCompare(right.operation),
      );
  }

  /**
   * Renders the retained measurements as CSV for offline analysis.
   *
   * @returns The CSV document.
   */
  toCsv(): string {
    const header = 'timestamp,transport,operation,durationMs,requestCount,responseBytes,succeeded';
    const rows = this.measurements().map((measurement) =>
      [
        measurement.timestamp,
        measurement.transport,
        measurement.operation,
        measurement.durationMs.toFixed(3),
        measurement.requestCount,
        measurement.responseBytes,
        measurement.succeeded,
      ].join(','),
    );

    return [header, ...rows].join('\n');
  }
}

/** One aggregated row of the client side measurement summary. */
export interface MetricsSummaryRow {
  readonly transport: ApiTransport;
  readonly operation: string;
  readonly samples: number;
  readonly medianDurationMs: number;
  readonly p95DurationMs: number;
  readonly meanResponseBytes: number;
  readonly meanRequestCount: number;
}

function percentile(values: readonly number[], fraction: number): number {
  if (values.length === 0) {
    return 0;
  }

  const ordered = [...values].sort((left, right) => left - right);
  const index = Math.min(ordered.length - 1, Math.max(0, Math.ceil(fraction * ordered.length) - 1));
  return ordered[index];
}

function mean(values: readonly number[]): number {
  return values.length === 0
    ? 0
    : values.reduce((total, value) => total + value, 0) / values.length;
}
