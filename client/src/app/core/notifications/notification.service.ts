import { inject, Injectable, signal } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import type { AppError } from '../errors/app-error';

/**
 * Presents transient feedback, and is the single place a failure becomes visible to a user.
 *
 * Because every failure has already been normalized into {@link AppError}, this service needs no
 * knowledge of which transport produced it — which is exactly the property the comparison depends on.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  /**
   * Reports a successful action.
   *
   * @param message The message to show.
   */
  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 4000,
      panelClass: 'notification-success',
    });
  }

  /**
   * Reports a failure.
   *
   * @param error The normalized failure.
   */
  failure(error: AppError): void {
    const suffix = error.correlationId ? ` (reference ${error.correlationId})` : '';
    this.snackBar.open(`${error.message}${suffix}`, 'Dismiss', {
      duration: 8000,
      panelClass: 'notification-failure',
    });
  }
}

/**
 * Tracks how many operations are in flight so the shell can show one busy indicator.
 *
 * A counter rather than a boolean, because overlapping requests are normal and the indicator must
 * only disappear when the last of them finishes.
 */
@Injectable({ providedIn: 'root' })
export class BusyService {
  private readonly inFlight = signal(0);

  /** Whether at least one operation is in flight. */
  readonly isBusy = signal(false);

  /** Registers the start of an operation. */
  start(): void {
    this.inFlight.update((count) => count + 1);
    this.isBusy.set(true);
  }

  /** Registers the end of an operation. */
  stop(): void {
    this.inFlight.update((count) => Math.max(0, count - 1));
    this.isBusy.set(this.inFlight() > 0);
  }
}
