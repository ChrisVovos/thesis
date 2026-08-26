import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import type { AppError } from '../../../core/errors/app-error';

/**
 * Renders the three states every data-loading screen has: loading, failed and empty.
 *
 * Centralising them means a screen never has to decide how a failure looks, and — because the error
 * has already been normalized — never has to know which transport produced it.
 */
@Component({
  selector: 'app-load-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    @if (loading()) {
      <mat-progress-bar mode="indeterminate" aria-label="Loading" data-testid="load-state-loading" />
    } @else if (error(); as failure) {
      <div class="state state-error" role="alert" data-testid="load-state-error">
        <mat-icon aria-hidden="true">error_outline</mat-icon>
        <div>
          <p class="message">{{ failure.message }}</p>
          @if (failure.correlationId) {
            <p class="reference">Reference {{ failure.correlationId }}</p>
          }
        </div>
        <button matButton="outlined" type="button" (click)="retry.emit()">Try again</button>
      </div>
    } @else if (empty()) {
      <div class="state state-empty" data-testid="load-state-empty">
        <mat-icon aria-hidden="true">inbox</mat-icon>
        <p class="message">{{ emptyMessage() }}</p>
      </div>
    }
  `,
  styles: `
    .state {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 2rem 1rem;
      justify-content: center;
      text-align: center;
    }

    .state-error { color: var(--mat-sys-error, #b3261e); }
    .state-empty { color: rgba(0, 0, 0, 0.6); }
    .message { margin: 0; }
    .reference { margin: 0.25rem 0 0; font-size: 0.75rem; opacity: 0.75; }
  `,
})
export class LoadState {
  /** Whether the screen is waiting for data. */
  readonly loading = input(false);

  /** The failure to report, when the last attempt failed. */
  readonly error = input<AppError | null>(null);

  /** Whether the screen loaded successfully but has nothing to show. */
  readonly empty = input(false);

  /** The message shown when there is nothing to show. */
  readonly emptyMessage = input('Nothing to show yet.');

  /** Raised when the user asks to retry a failed load. */
  readonly retry = output<void>();
}
