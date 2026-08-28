import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { ItemsGateway } from '../../data-access/gateways/items.gateway';
import { API_TRANSPORTS } from '../../core/transport/api-transport';

/**
 * The benchmark harness.
 *
 * It drives the same gateway calls the item bank makes, once per transport, and reports what each
 * one cost. Because the gateway contract is transport agnostic, the loop below contains no branch on
 * which surface is active — it simply switches the toolbar selection and runs the identical code.
 */
@Component({
  selector: 'app-benchmark-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatTableModule,
  ],
  template: `
    <header class="page-header">
      <div>
        <h1>Transport benchmark</h1>
        <p class="subtitle">
          Runs the same operations over REST and over GraphQL and reports the client side cost of
          each. The server records its own figures at <code>/api/v1/benchmark</code>.
        </p>
      </div>
    </header>

    <mat-card class="controls">
      <mat-card-content>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Iterations per transport</mat-label>
          <input
            matInput
            type="number"
            min="1"
            max="200"
            data-testid="benchmark-iterations"
            [ngModel]="iterations()"
            (ngModelChange)="iterations.set($event)"
          />
        </mat-form-field>

        <button
          matButton="filled"
          type="button"
          data-testid="run-benchmark"
          [disabled]="running()"
          (click)="run()"
        >
          <mat-icon>play_arrow</mat-icon>
          Run
        </button>

        <button matButton="outlined" type="button" (click)="clear()">Clear</button>
        <button matButton type="button" (click)="download()" data-testid="download-measurements">
          <mat-icon>download</mat-icon>
          Export CSV
        </button>
      </mat-card-content>
    </mat-card>

    @if (summary().length > 0) {
      <mat-card class="results">
        <table mat-table [dataSource]="summary()" data-testid="benchmark-table">
          <ng-container matColumnDef="operation">
            <th mat-header-cell *matHeaderCellDef>Operation</th>
            <td mat-cell *matCellDef="let row">{{ row.operation }}</td>
          </ng-container>

          <ng-container matColumnDef="transport">
            <th mat-header-cell *matHeaderCellDef>Transport</th>
            <td mat-cell *matCellDef="let row">{{ row.transport }}</td>
          </ng-container>

          <ng-container matColumnDef="samples">
            <th mat-header-cell *matHeaderCellDef>Samples</th>
            <td mat-cell *matCellDef="let row">{{ row.samples }}</td>
          </ng-container>

          <ng-container matColumnDef="median">
            <th mat-header-cell *matHeaderCellDef>Median (ms)</th>
            <td mat-cell *matCellDef="let row">{{ row.medianDurationMs | number: '1.1-1' }}</td>
          </ng-container>

          <ng-container matColumnDef="p95">
            <th mat-header-cell *matHeaderCellDef>95th percentile (ms)</th>
            <td mat-cell *matCellDef="let row">{{ row.p95DurationMs | number: '1.1-1' }}</td>
          </ng-container>

          <ng-container matColumnDef="bytes">
            <th mat-header-cell *matHeaderCellDef>Mean payload (bytes)</th>
            <td mat-cell *matCellDef="let row">{{ row.meanResponseBytes | number: '1.0-0' }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
      </mat-card>
    }
  `,
  styles: `
    .controls {
      margin-bottom: 1rem;

      mat-card-content { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
    }

    code {
      padding: 0.1rem 0.35rem;
      border-radius: 4px;
      background: var(--app-surface-muted);
      font-size: 0.875em;
    }
  `,
})
export class BenchmarkPage {
  private readonly items = inject(ItemsGateway);
  private readonly metrics = inject(MetricsCollector);
  private readonly transport = inject(TransportService);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['operation', 'transport', 'samples', 'median', 'p95', 'bytes'];

  /** How many times each operation is run per transport. */
  protected readonly iterations = signal(10);

  /** Whether a run is in progress. */
  protected readonly running = signal(false);

  /** The aggregated results of the retained measurements. */
  protected readonly summary = computed(() => {
    this.version();
    return this.metrics.summarize();
  });

  private readonly version = signal(0);

  /** Runs the benchmark over both transports. */
  protected async run(): Promise<void> {
    this.running.set(true);
    const original = this.transport.active();

    try {
      for (const surface of API_TRANSPORTS) {
        this.transport.use(surface);

        for (let iteration = 0; iteration < this.iterations(); iteration++) {
          const page = await firstValueFrom(
            this.items.search({ page: 1, pageSize: 20, statuses: ['Published'] }),
          );

          if (page.items.length > 0) {
            await firstValueFrom(this.items.getById(page.items[0].id));
          }
        }
      }

      this.notifications.success('The benchmark run finished.');
    } catch {
      this.notifications.failure({
        code: 'benchmark.failed',
        message: 'The benchmark run did not complete.',
        kind: 'failure',
      });
    } finally {
      this.transport.use(original);
      this.version.update((value) => value + 1);
      this.running.set(false);
    }
  }

  /** Discards every retained measurement. */
  protected clear(): void {
    this.metrics.clear();
    this.version.update((value) => value + 1);
  }

  /** Downloads the retained measurements as CSV. */
  protected download(): void {
    const blob = new Blob([this.metrics.toCsv()], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'client-measurements.csv';
    anchor.click();
    URL.revokeObjectURL(url);
  }
}
