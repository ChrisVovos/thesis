import { Injectable, signal, type Signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { isApiTransport, type ApiTransport } from './api-transport';

/**
 * Owns the currently selected API transport.
 *
 * This is the only writable owner of that value in the entire application. Everything else — the
 * gateway router, the data-loading screens, the metrics collector — reads {@link active} and reacts,
 * which is what allows the same screen to be exercised over both transports within one session.
 */
@Injectable({ providedIn: 'root' })
export class TransportService {
  /** The local storage key under which the choice survives a reload. */
  static readonly StorageKey = 'api-transport';

  private readonly current = signal<ApiTransport>(TransportService.resolveInitial());

  /** The transport every gateway call currently uses. */
  readonly active: Signal<ApiTransport> = this.current.asReadonly();

  /**
   * Switches the active transport and persists the choice.
   *
   * @param transport The transport to switch to.
   */
  use(transport: ApiTransport): void {
    if (!isApiTransport(transport) || transport === this.current()) {
      return;
    }

    const previous = this.current();
    this.current.set(transport);

    try {
      localStorage.setItem(TransportService.StorageKey, transport);
    } catch {
      // A browser with storage disabled still works; the choice simply does not survive a reload.
    }

    // Recorded so a benchmark run can be reconstructed from the console transcript alone.
    console.info('[transport] switched', { from: previous, to: transport, at: new Date().toISOString() });
  }

  /**
   * Resolves the transport to start with.
   *
   * A stored value is only trusted when it names a transport this build supports; anything else is
   * discarded rather than propagated into the gateway layer.
   */
  private static resolveInitial(): ApiTransport {
    if (environment.production && !environment.showTransportSelector) {
      return environment.defaultTransport;
    }

    try {
      const stored = localStorage.getItem(TransportService.StorageKey);
      if (isApiTransport(stored)) {
        return stored;
      }
      if (stored !== null) {
        localStorage.removeItem(TransportService.StorageKey);
      }
    } catch {
      // Fall through to the configured default.
    }

    return isApiTransport(environment.defaultTransport) ? environment.defaultTransport : 'rest';
  }
}
