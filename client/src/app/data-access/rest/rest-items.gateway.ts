import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, type Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  Category,
  ItemDetail,
  ItemDraft,
  ItemQuery,
  ItemSummary,
  ItemTransition,
  ItemVersion,
  Tag,
} from '../../shared/models/item.models';
import { toPagedResult, type PagedResult } from '../../shared/models/paging.models';
import { ItemsGateway, TaxonomyGateway } from '../gateways/items.gateway';
import { measured } from '../measurement';

/** The offset-paged envelope the REST surface returns for a list resource. */
interface RestPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

/** The route segment each lifecycle transition maps to. */
const TRANSITION_ROUTES: Readonly<Record<ItemTransition, string>> = {
  submit: 'submit',
  approve: 'approve',
  returnToDraft: 'return-to-draft',
  publish: 'publish',
  retire: 'retire',
};

/**
 * The REST implementation of {@link ItemsGateway}.
 *
 * Every read is one request that returns a resource shaped for the screen. Where a screen needs less
 * than the resource offers — the grid does not use the rubric, for instance — the surplus is still
 * transferred, and that over-fetching is one of the quantities the study measures.
 */
@Injectable({ providedIn: 'root' })
export class RestItemsGateway extends ItemsGateway {
  private readonly http = inject(HttpClient);
  private readonly metrics = inject(MetricsCollector);
  private readonly baseUrl = `${environment.restBaseUrl}/items`;

  /** @inheritdoc */
  override search(query: ItemQuery): Observable<PagedResult<ItemSummary>> {
    return measured(this.metrics, 'rest', 'items.search', 1, () =>
      this.http
        .get<RestPage<ItemSummary>>(this.baseUrl, { params: RestItemsGateway.toParams(query) })
        .pipe(map((page) => toPagedResult(page.items, page.totalCount, page.page, page.pageSize))),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<ItemDetail> {
    return measured(this.metrics, 'rest', 'items.getById', 1, () =>
      this.http.get<ItemDetail>(`${this.baseUrl}/${id}`),
    );
  }

  /** @inheritdoc */
  override getVersions(id: string): Observable<readonly ItemVersion[]> {
    return measured(this.metrics, 'rest', 'items.getVersions', 1, () =>
      this.http.get<readonly ItemVersion[]>(`${this.baseUrl}/${id}/versions`),
    );
  }

  /** @inheritdoc */
  override create(draft: ItemDraft): Observable<string> {
    return measured(this.metrics, 'rest', 'items.create', 1, () =>
      this.http
        .post<{ id: string }>(this.baseUrl, RestItemsGateway.toBody(draft))
        .pipe(map((created) => created.id)),
    );
  }

  /** @inheritdoc */
  override update(id: string, draft: ItemDraft): Observable<void> {
    return measured(this.metrics, 'rest', 'items.update', 1, () =>
      this.http.put<void>(`${this.baseUrl}/${id}`, { itemId: id, ...RestItemsGateway.toBody(draft) }),
    );
  }

  /** @inheritdoc */
  override remove(id: string): Observable<void> {
    return measured(this.metrics, 'rest', 'items.delete', 1, () =>
      this.http.delete<void>(`${this.baseUrl}/${id}`),
    );
  }

  /** @inheritdoc */
  override transition(id: string, transition: ItemTransition): Observable<void> {
    return measured(this.metrics, 'rest', `items.${transition}`, 1, () =>
      this.http.post<void>(`${this.baseUrl}/${id}/${TRANSITION_ROUTES[transition]}`, null),
    );
  }

  private static toParams(query: ItemQuery): HttpParams {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }
    if (query.sortDescending) {
      params = params.set('sortDescending', true);
    }
    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.authorId) {
      params = params.set('authorId', query.authorId);
    }

    for (const type of query.types ?? []) {
      params = params.append('type', type);
    }
    for (const status of query.statuses ?? []) {
      params = params.append('status', status);
    }
    for (const difficulty of query.difficulties ?? []) {
      params = params.append('difficulty', difficulty);
    }
    for (const tagId of query.tagIds ?? []) {
      params = params.append('tagId', tagId);
    }

    return params;
  }

  private static toBody(draft: ItemDraft): Record<string, unknown> {
    return {
      type: draft.type,
      stem: draft.stem,
      difficulty: draft.difficulty,
      categoryId: draft.categoryId,
      maximumScore: draft.maximumScore,
      options: draft.options?.map((option) => ({
        text: option.text,
        isCorrect: option.isCorrect,
        feedback: option.feedback ?? null,
      })),
      rubric: draft.rubric ?? null,
      sampleAnswer: draft.sampleAnswer ?? null,
      tagIds: draft.tagIds ?? [],
    };
  }
}

/** The REST implementation of {@link TaxonomyGateway}. */
@Injectable({ providedIn: 'root' })
export class RestTaxonomyGateway extends TaxonomyGateway {
  private readonly http = inject(HttpClient);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override categories(): Observable<readonly Category[]> {
    return measured(this.metrics, 'rest', 'taxonomy.categories', 1, () =>
      this.http.get<readonly Category[]>(`${environment.restBaseUrl}/categories`),
    );
  }

  /** @inheritdoc */
  override tags(): Observable<readonly Tag[]> {
    return measured(this.metrics, 'rest', 'taxonomy.tags', 1, () =>
      this.http.get<readonly Tag[]>(`${environment.restBaseUrl}/tags`),
    );
  }

  /** @inheritdoc */
  override createTag(name: string): Observable<string> {
    return measured(this.metrics, 'rest', 'taxonomy.createTag', 1, () =>
      this.http
        .post<{ id: string }>(`${environment.restBaseUrl}/tags`, { name })
        .pipe(map((created) => created.id)),
    );
  }
}
