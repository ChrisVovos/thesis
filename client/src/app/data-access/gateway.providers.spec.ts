import { signal } from '@angular/core';
import type { TransportService } from '../core/transport/transport.service';
import { createTransportRouter } from './gateway.providers';

/** A minimal contract, standing in for a real gateway. */
abstract class SampleGateway {
  abstract describe(): string;
  abstract echo(value: number): number;
}

class RestSample extends SampleGateway {
  override describe(): string {
    return 'rest';
  }

  override echo(value: number): number {
    return value;
  }
}

class GraphQlSample extends SampleGateway {
  override describe(): string {
    return 'graphql';
  }

  override echo(value: number): number {
    return value * 2;
  }
}

/**
 * The router is the single point at which a call is bound to a transport. If it ever resolved the
 * implementation once, at injection time, the toolbar selector would stop affecting screens that are
 * already on the page — and every measurement taken afterwards would be attributed to the wrong
 * surface.
 */
describe('transport router', () => {
  function build() {
    const active = signal<'rest' | 'graphql'>('rest');
    const transport = { active: active.asReadonly() } as unknown as TransportService;
    const gateway = createTransportRouter<SampleGateway>(
      transport,
      new RestSample(),
      new GraphQlSample(),
    );
    return { active, gateway };
  }

  it('routes to the REST implementation by default', () => {
    const { gateway } = build();

    expect(gateway.describe()).toBe('rest');
  });

  it('routes to the GraphQL implementation after a switch', () => {
    const { active, gateway } = build();

    active.set('graphql');

    expect(gateway.describe()).toBe('graphql');
  });

  it('resolves per call, not per injection', () => {
    const { active, gateway } = build();

    expect(gateway.echo(21)).toBe(21);
    active.set('graphql');
    expect(gateway.echo(21)).toBe(42);
    active.set('rest');
    expect(gateway.echo(21)).toBe(21);
  });

  it('routes methods added to the contract without any further wiring', () => {
    const { active, gateway } = build();

    // Both implementations satisfy the whole contract; the router forwards every member of it,
    // which is exactly the property a hand written delegating class could silently lose.
    const members: (keyof SampleGateway)[] = ['describe', 'echo'];
    active.set('graphql');

    expect(members.every((member) => typeof gateway[member] === 'function')).toBe(true);
  });
});
