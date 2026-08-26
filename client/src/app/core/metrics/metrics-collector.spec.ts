import { MetricsCollector } from './metrics-collector';

describe('MetricsCollector', () => {
  function sample(transport: 'rest' | 'graphql', durationMs: number, bytes: number) {
    return {
      timestamp: new Date().toISOString(),
      transport,
      operation: 'items.search',
      durationMs,
      requestCount: 1,
      responseBytes: bytes,
      succeeded: true,
    } as const;
  }

  it('retains what it is given, oldest first', () => {
    const collector = new MetricsCollector();

    collector.record(sample('rest', 10, 100));
    collector.record(sample('graphql', 20, 50));

    expect(collector.samples().map((entry) => entry.transport)).toEqual(['rest', 'graphql']);
  });

  it('summarises each transport and operation separately', () => {
    const collector = new MetricsCollector();

    collector.record(sample('rest', 10, 1000));
    collector.record(sample('rest', 30, 1000));
    collector.record(sample('graphql', 20, 400));

    const summary = collector.summarize();

    expect(summary).toHaveLength(2);
    const rest = summary.find((row) => row.transport === 'rest');
    const graphql = summary.find((row) => row.transport === 'graphql');
    expect(rest?.samples).toBe(2);
    expect(rest?.meanResponseBytes).toBe(1000);
    expect(graphql?.medianDurationMs).toBe(20);
  });

  it('reports zeroes rather than failing on an empty set', () => {
    expect(new MetricsCollector().summarize()).toEqual([]);
  });

  it('renders a CSV with one header and one row per measurement', () => {
    const collector = new MetricsCollector();
    collector.record(sample('rest', 12.345, 900));

    const rows = collector.toCsv().split('\n');

    expect(rows[0]).toContain('transport,operation');
    expect(rows).toHaveLength(2);
    expect(rows[1]).toContain('rest,items.search');
  });

  it('clears every retained measurement', () => {
    const collector = new MetricsCollector();
    collector.record(sample('rest', 1, 1));

    collector.clear();

    expect(collector.samples()).toEqual([]);
  });

  it('never grows past its capacity', () => {
    const collector = new MetricsCollector();

    for (let index = 0; index < MetricsCollector.Capacity + 25; index++) {
      collector.record(sample('rest', index, index));
    }

    expect(collector.samples()).toHaveLength(MetricsCollector.Capacity);
    expect(collector.samples()[0].durationMs).toBe(25);
  });
});
