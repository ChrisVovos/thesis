import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { environment } from '../../../../environments/environment';
import { API_TRANSPORTS, type ApiTransport } from '../../../core/transport/api-transport';
import { TransportService } from '../../../core/transport/transport.service';

/** The label shown for each transport. */
const LABELS: Readonly<Record<ApiTransport, string>> = {
  rest: 'REST',
  graphql: 'GraphQL',
};

/**
 * The toolbar control that decides which API surface every gateway call is sent over.
 *
 * It is rendered once, in the application shell, and it holds no business logic, no HTTP and no
 * knowledge of any gateway: it reads and writes {@link TransportService} and nothing else.
 */
@Component({
  selector: 'app-transport-selector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatSelectModule],
  template: `
    @if (visible) {
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="transport-selector">
        <mat-label>API transport</mat-label>
        <mat-select
          data-testid="transport-selector"
          aria-label="API transport"
          [value]="active()"
          (selectionChange)="select($event.value)"
        >
          @for (option of options; track option) {
            <mat-option [value]="option">{{ label(option) }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <span class="visually-hidden" role="status" aria-live="polite" data-testid="transport-status">
        {{ announcement() }}
      </span>
    }
  `,
  styles: `
    .transport-selector {
      width: 10rem;
      font-size: 0.85rem;
    }

    .visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      margin: -1px;
      padding: 0;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }
  `,
})
export class TransportSelector {
  private readonly transport = inject(TransportService);

  /** The transports offered by the control. */
  protected readonly options = API_TRANSPORTS;

  /** Whether this build renders the control at all. */
  protected readonly visible = environment.showTransportSelector;

  /** The currently selected transport. */
  protected readonly active = this.transport.active;

  /** The message announced to assistive technology after a change. */
  protected readonly announcement = computed(
    () => `API transport is now ${LABELS[this.active()]}.`,
  );

  /**
   * Renders the label of a transport.
   *
   * @param transport The transport to label.
   * @returns The display label.
   */
  protected label(transport: ApiTransport): string {
    return LABELS[transport];
  }

  /**
   * Switches the active transport.
   *
   * @param transport The transport chosen by the user.
   */
  protected select(transport: ApiTransport): void {
    this.transport.use(transport);
  }
}
