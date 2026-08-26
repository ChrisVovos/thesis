import { defer, tap, type Observable } from 'rxjs';
import type { MetricsCollector } from '../core/metrics/metrics-collector';
import type { ApiTransport } from '../core/transport/api-transport';

/**
 * Wraps a gateway call so that its wall-clock duration, request count and payload size are recorded.
 *
 * The payload size is the length of the JSON representation of the mapped result. It is an
 * approximation of the uncompressed bytes on the wire — the browser does not expose the real figure
 * to script — and it is computed identically for both transports, so the comparison between them
 * remains fair even though the absolute value is indicative rather than exact. The server side
 * measurement in `RequestMetricsMiddleware` records the true byte count.
 *
 * @param collector The measurement store.
 * @param transport The transport serving the call.
 * @param operation The logical operation being measured.
 * @param requestCount The number of network round trips the call makes.
 * @param source A factory for the underlying call, deferred so the clock starts on subscription.
 * @returns The measured observable.
 */
export function measured<T>(
  collector: MetricsCollector,
  transport: ApiTransport,
  operation: string,
  requestCount: number,
  source: () => Observable<T>,
): Observable<T> {
  return defer(() => {
    const startedAt = performance.now();

    return source().pipe(
      tap({
        next: (value) =>
          collector.record({
            timestamp: new Date().toISOString(),
            transport,
            operation,
            durationMs: performance.now() - startedAt,
            requestCount,
            responseBytes: approximateSize(value),
            succeeded: true,
          }),
        error: () =>
          collector.record({
            timestamp: new Date().toISOString(),
            transport,
            operation,
            durationMs: performance.now() - startedAt,
            requestCount,
            responseBytes: 0,
            succeeded: false,
          }),
      }),
    );
  });
}

function approximateSize(value: unknown): number {
  if (value === undefined || value === null) {
    return 0;
  }

  try {
    return new TextEncoder().encode(JSON.stringify(value)).length;
  } catch {
    return 0;
  }
}
