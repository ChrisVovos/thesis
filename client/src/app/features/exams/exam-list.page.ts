import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { isAppError } from '../../core/errors/app-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { ExamsGateway } from '../../data-access/gateways/exams.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { StatusChip } from '../../shared/components/status-chip/status-chip';
import { Permissions } from '../../shared/models/auth.models';
import { EXAM_STATUSES, type ExamQuery, type ExamStatus } from '../../shared/models/exam.models';
import { NewExamDialog } from './new-exam.dialog';

/** The exam list. */
@Component({
  selector: 'app-exam-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatSelectModule,
    MatTableModule,
    RouterLink,
    StatusChip,
  ],
  template: `
    <header class="page-header">
      <div>
        <h1>Examinations</h1>
        <p class="subtitle">{{ totalCount() }} exams match the current filters.</p>
      </div>

      @if (canCreate()) {
        <button matButton="filled" type="button" (click)="create()" data-testid="create-exam">
          <mat-icon>add</mat-icon>
          New exam
        </button>
      }
    </header>

    <mat-card class="filters">
      <mat-card-content>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Search</mat-label>
          <input
            matInput
            type="search"
            data-testid="exam-search"
            [ngModel]="criteria().search ?? ''"
            (ngModelChange)="search($event)"
          />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Status</mat-label>
          <mat-select
            multiple
            data-testid="exam-filter-status"
            [ngModel]="criteria().statuses ?? []"
            (ngModelChange)="filterStatuses($event)"
          >
            @for (status of statuses; track status) {
              <mat-option [value]="status">{{ status }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </mat-card-content>
    </mat-card>

    <app-load-state
      [loading]="page.isLoading()"
      [error]="failure()"
      [empty]="!page.isLoading() && !failure() && rows().length === 0"
      emptyMessage="No exams match the current filters."
      (retry)="page.reload()"
    />

    @if (rows().length > 0) {
      <mat-card class="results">
        <table mat-table [dataSource]="rows()" data-testid="exam-table">
          <ng-container matColumnDef="title">
            <th mat-header-cell *matHeaderCellDef>Title</th>
            <td mat-cell *matCellDef="let exam">
              <a [routerLink]="['/exams', exam.id]">{{ exam.title }}</a>
            </td>
          </ng-container>

          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let exam"><app-status-chip [value]="exam.status" /></td>
          </ng-container>

          <ng-container matColumnDef="composition">
            <th mat-header-cell *matHeaderCellDef>Composition</th>
            <td mat-cell *matCellDef="let exam">
              {{ exam.sectionCount }} sections · {{ exam.itemCount }} items · {{ exam.totalScore }} points
            </td>
          </ng-container>

          <ng-container matColumnDef="owner">
            <th mat-header-cell *matHeaderCellDef>Owner</th>
            <td mat-cell *matCellDef="let exam">{{ exam.ownerName }}</td>
          </ng-container>

          <ng-container matColumnDef="created">
            <th mat-header-cell *matHeaderCellDef>Created</th>
            <td mat-cell *matCellDef="let exam">{{ exam.createdAtUtc | date: 'shortDate' }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>

        <mat-paginator
          [length]="totalCount()"
          [pageIndex]="criteria().page - 1"
          [pageSize]="criteria().pageSize"
          [pageSizeOptions]="[10, 20, 50]"
          (page)="changePage($event)"
          aria-label="Select a page of exams"
        />
      </mat-card>
    }
  `,
  styles: `
    .composition { color: var(--app-text-muted); }
  `,
})
export class ExamListPage {
  private readonly exams = inject(ExamsGateway);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthStore);
  private readonly transport = inject(TransportService);

  protected readonly statuses = EXAM_STATUSES;
  protected readonly columns = ['title', 'status', 'composition', 'owner', 'created'];

  /** The criteria currently applied. */
  protected readonly criteria = signal<ExamQuery>({ page: 1, pageSize: 20 });

  /** Whether the signed-in user may assemble exams. */
  protected readonly canCreate = computed(() => this.auth.has(Permissions.ExamsCreate));

  /** The current page of exams; re-runs when the transport changes. */
  protected readonly page = rxResource({
    params: () => ({ criteria: this.criteria(), transport: this.transport.active() }),
    stream: ({ params }) => this.exams.search(params.criteria),
  });

  protected readonly rows = computed(() => this.page.value()?.items ?? []);
  protected readonly totalCount = computed(() => this.page.value()?.totalCount ?? 0);
  protected readonly failure = computed(() => {
    const error = this.page.error();
    return isAppError(error) ? error : null;
  });

  /** Applies a new free-text search term. */
  protected search(term: string): void {
    this.criteria.update((current) => ({ ...current, search: term || undefined, page: 1 }));
  }

  /** Restricts the result to the supplied statuses. */
  protected filterStatuses(statuses: readonly ExamStatus[]): void {
    this.criteria.update((current) => ({ ...current, statuses, page: 1 }));
  }

  /** Moves to another page. */
  protected changePage(event: PageEvent): void {
    this.criteria.update((current) => ({
      ...current,
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    }));
  }

  /** Collects the details of a new exam and opens the builder for it. */
  protected async create(): Promise<void> {
    const draft = await firstValueFrom(this.dialog.open(NewExamDialog).afterClosed());
    if (!draft) {
      return;
    }

    try {
      const id = await firstValueFrom(this.exams.create(draft));
      this.notifications.success('The exam was created.');
      await this.router.navigate(['/exams', id]);
    } catch (error: unknown) {
      this.notifications.failure(
        isAppError(error)
          ? error
          : { code: 'client.unexpected', message: 'The exam could not be created.', kind: 'failure' },
      );
    }
  }
}
