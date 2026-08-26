import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthStore } from '../../core/auth/auth.store';
import { isAppError } from '../../core/errors/app-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { TransportService } from '../../core/transport/transport.service';
import { ItemsGateway, TaxonomyGateway } from '../../data-access/gateways/items.gateway';
import { LoadState } from '../../shared/components/load-state/load-state';
import { StatusChip } from '../../shared/components/status-chip/status-chip';
import { Permissions } from '../../shared/models/auth.models';
import {
  DIFFICULTY_LEVELS,
  ITEM_STATUSES,
  ITEM_TYPES,
  type DifficultyLevel,
  type ItemQuery,
  type ItemStatus,
  type ItemSummary,
  type ItemTransition,
  type ItemType,
} from '../../shared/models/item.models';
import { emptyPage } from '../../shared/models/paging.models';

/** The item bank: search, filter, sort, page and act on items. */
@Component({
  selector: 'app-item-list-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    LoadState,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
    StatusChip,
  ],
  templateUrl: './item-list.page.html',
  styleUrl: './item-list.page.scss',
})
export class ItemListPage {
  private readonly items = inject(ItemsGateway);
  private readonly taxonomy = inject(TaxonomyGateway);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthStore);

  /** The active transport; every request re-runs when it changes. */
  protected readonly transport = inject(TransportService);

  /** The options offered by the filter controls. */
  protected readonly itemTypes = ITEM_TYPES;
  protected readonly itemStatuses = ITEM_STATUSES;
  protected readonly difficulties = DIFFICULTY_LEVELS;
  protected readonly pageSizes = [10, 20, 50, 100];
  protected readonly columns = ['stem', 'type', 'status', 'difficulty', 'category', 'score', 'actions'];

  /** The criteria currently applied. */
  protected readonly criteria = signal<ItemQuery>({ page: 1, pageSize: 20 });

  /** Whether the signed-in user may author items. */
  protected readonly canCreate = computed(() => this.auth.has(Permissions.ItemsCreate));
  protected readonly canUpdate = computed(() => this.auth.has(Permissions.ItemsUpdate));
  protected readonly canDelete = computed(() => this.auth.has(Permissions.ItemsDelete));
  protected readonly canSubmit = computed(() => this.auth.has(Permissions.ItemsSubmit));
  protected readonly canReview = computed(() => this.auth.has(Permissions.ItemsReview));
  protected readonly canPublish = computed(() => this.auth.has(Permissions.ItemsPublish));

  /**
   * The current page of the item bank.
   *
   * The active transport is part of the request parameters, which is what makes flipping the toolbar
   * selector re-run the very same query over the other surface with no manual reload.
   */
  protected readonly page = rxResource({
    params: () => ({ criteria: this.criteria(), transport: this.transport.active() }),
    stream: ({ params }) => this.items.search(params.criteria),
  });

  /** The categories available as a filter. */
  protected readonly categories = rxResource({
    params: () => ({ transport: this.transport.active() }),
    stream: () => this.taxonomy.categories(),
  });

  /** The rows currently displayed. */
  protected readonly rows = computed<readonly ItemSummary[]>(
    () => this.page.value()?.items ?? emptyPage<ItemSummary>().items,
  );

  /** The number of rows matching the criteria across all pages. */
  protected readonly totalCount = computed(() => this.page.value()?.totalCount ?? 0);

  /** The normalized failure of the last load, when it failed. */
  protected readonly failure = computed(() => {
    const error = this.page.error();
    return isAppError(error) ? error : null;
  });

  /** Applies a new free-text search term. */
  protected search(term: string): void {
    this.criteria.update((current) => ({ ...current, search: term || undefined, page: 1 }));
  }

  /** Restricts the result to the supplied answer shapes. */
  protected filterTypes(types: readonly ItemType[]): void {
    this.criteria.update((current) => ({ ...current, types, page: 1 }));
  }

  /** Restricts the result to the supplied lifecycle statuses. */
  protected filterStatuses(statuses: readonly ItemStatus[]): void {
    this.criteria.update((current) => ({ ...current, statuses, page: 1 }));
  }

  /** Restricts the result to the supplied difficulty levels. */
  protected filterDifficulties(difficulties: readonly DifficultyLevel[]): void {
    this.criteria.update((current) => ({ ...current, difficulties, page: 1 }));
  }

  /** Restricts the result to one category. */
  protected filterCategory(categoryId: string | null): void {
    this.criteria.update((current) => ({ ...current, categoryId: categoryId ?? undefined, page: 1 }));
  }

  /** Sorts by the supplied column, toggling direction when it is already the active one. */
  protected sortBy(column: string): void {
    this.criteria.update((current) => ({
      ...current,
      sortBy: column,
      sortDescending: current.sortBy === column ? !current.sortDescending : false,
      page: 1,
    }));
  }

  /** Moves to another page. */
  protected changePage(event: PageEvent): void {
    this.criteria.update((current) => ({
      ...current,
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    }));
  }

  /** Re-runs the current query. */
  protected reload(): void {
    this.page.reload();
  }

  /**
   * Requests a lifecycle transition and refreshes the page.
   *
   * @param item The item to transition.
   * @param transition The transition to request.
   */
  protected async transition(item: ItemSummary, transition: ItemTransition): Promise<void> {
    try {
      await firstValueFrom(this.items.transition(item.id, transition));
      this.notifications.success('The item was updated.');
      this.page.reload();
    } catch (error: unknown) {
      this.report(error);
    }
  }

  /**
   * Logically removes an item and refreshes the page.
   *
   * @param item The item to remove.
   */
  protected async remove(item: ItemSummary): Promise<void> {
    try {
      await firstValueFrom(this.items.remove(item.id));
      this.notifications.success('The item was deleted.');
      this.page.reload();
    } catch (error: unknown) {
      this.report(error);
    }
  }

  private report(error: unknown): void {
    this.notifications.failure(
      isAppError(error)
        ? error
        : { code: 'client.unexpected', message: 'The operation failed.', kind: 'failure' },
    );
  }
}
