import { ApplicationRef, Injectable, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { rxResource } from '@angular/core/rxjs-interop';
import { of, type Observable } from 'rxjs';
import { TransportService } from '../../core/transport/transport.service';
import type { ApiTransport } from '../../core/transport/api-transport';

/** A transport service a test can drive directly. */
@Injectable()
class StubTransportService {
  readonly current: WritableSignal<ApiTransport> = signal<ApiTransport>('rest');
  readonly active = this.current.asReadonly();

  use(transport: ApiTransport): void {
    this.current.set(transport);
  }
}

/**
 * Pins down the reactive refetch rule that the whole benchmark depends on.
 *
 * Every data-loading screen includes the active transport in the parameters of its resource. Without
 * that, flipping the toolbar selector would change which gateway *future* calls use but leave the
 * current screen showing data fetched over the other surface — and a reviewer would have no way of
 * telling which transport produced what they are looking at.
 */
describe('transport-driven refetch', () => {
  it('re-runs a screen resource when the transport changes', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: TransportService, useClass: StubTransportService }],
    });

    const transport = TestBed.inject(TransportService) as unknown as StubTransportService;
    const applicationRef = TestBed.inject(ApplicationRef);
    const calls: ApiTransport[] = [];

    const resource = TestBed.runInInjectionContext(() =>
      rxResource({
        params: () => ({ transport: transport.active() }),
        stream: ({ params }): Observable<string> => {
          calls.push(params.transport);
          return of(`loaded over ${params.transport}`);
        },
      }),
    );

    await applicationRef.whenStable();

    expect(calls).toEqual(['rest']);
    expect(resource.value()).toBe('loaded over rest');

    transport.use('graphql');
    await applicationRef.whenStable();

    expect(calls).toEqual(['rest', 'graphql']);
    expect(resource.value()).toBe('loaded over graphql');
  });
});
