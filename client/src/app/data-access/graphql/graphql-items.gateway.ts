import { inject, Injectable } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { map, type Observable } from 'rxjs';
import { MetricsCollector } from '../../core/metrics/metrics-collector';
import type {
  Category,
  DifficultyLevel,
  ItemDetail,
  ItemDraft,
  ItemOption,
  ItemQuery,
  ItemStatus,
  ItemSummary,
  ItemTransition,
  ItemType,
  ItemVersion,
  Tag,
} from '../../shared/models/item.models';
import { toPagedResult, type PagedResult } from '../../shared/models/paging.models';
import { ItemsGateway, TaxonomyGateway } from '../gateways/items.gateway';
import { measured } from '../measurement';
import { fromGraphQlEnum, toGraphQlEnum, toGraphQlEnums } from './enum-mapping';
import { runMutation, runQuery } from './graphql-execution';
import {
  APPROVE_ITEM,
  CATEGORIES,
  CREATE_ITEM,
  CREATE_TAG,
  DELETE_ITEM,
  ITEM_BY_ID,
  ITEM_VERSIONS,
  PUBLISH_ITEM,
  RETIRE_ITEM,
  RETURN_ITEM_TO_DRAFT,
  SEARCH_ITEMS,
  SUBMIT_ITEM,
  TAGS,
  UPDATE_ITEM,
} from './item.documents';

/** The raw shape the schema returns for an item summary, before enum names are translated. */
interface RawItemSummary extends Omit<ItemSummary, 'type' | 'status' | 'difficulty'> {
  readonly type: string;
  readonly status: string;
  readonly difficulty: string;
}

interface RawItemVersion extends Omit<ItemVersion, 'difficulty'> {
  readonly difficulty: string;
}

interface RawItemDetail {
  readonly summary: RawItemSummary;
  readonly options: readonly ItemOption[];
  readonly rubricGuidance: string | null;
  readonly rubricMinimumWords: number | null;
  readonly rubricMaximumWords: number | null;
  readonly sampleAnswer: string | null;
  readonly versions: readonly RawItemVersion[];
}

interface RawPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
}

/** The mutation each lifecycle transition maps to. */
const TRANSITION_DOCUMENTS = {
  submit: SUBMIT_ITEM,
  approve: APPROVE_ITEM,
  returnToDraft: RETURN_ITEM_TO_DRAFT,
  publish: PUBLISH_ITEM,
  retire: RETIRE_ITEM,
} as const;

/**
 * The GraphQL implementation of {@link ItemsGateway}.
 *
 * The screen asks for exactly the fields it renders, so the response carries no surplus. In exchange
 * the gateway pays for translating enum names, which the REST wire format does not require. Both
 * effects are visible in the measurements, which is the point of implementing the contract twice.
 */
@Injectable({ providedIn: 'root' })
export class GraphQlItemsGateway extends ItemsGateway {
  private readonly apollo = inject(Apollo);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override search(query: ItemQuery): Observable<PagedResult<ItemSummary>> {
    const criteria = {
      page: query.page,
      pageSize: query.pageSize,
      search: query.search ?? null,
      sortBy: query.sortBy ?? null,
      sortDescending: query.sortDescending ?? false,
      types: toGraphQlEnums(query.types) ?? null,
      statuses: toGraphQlEnums(query.statuses) ?? null,
      difficulties: toGraphQlEnums(query.difficulties) ?? null,
      categoryId: query.categoryId ?? null,
      tagIds: query.tagIds?.length ? [...query.tagIds] : null,
      authorId: query.authorId ?? null,
    };

    return measured(this.metrics, 'graphql', 'items.search', 1, () =>
      runQuery<{ searchItems: RawPage<RawItemSummary> }, PagedResult<ItemSummary>>(
        this.apollo,
        SEARCH_ITEMS,
        (data) =>
          toPagedResult(
            data.searchItems.items.map(GraphQlItemsGateway.toSummary),
            data.searchItems.totalCount,
            data.searchItems.page,
            data.searchItems.pageSize,
          ),
        { criteria },
      ),
    );
  }

  /** @inheritdoc */
  override getById(id: string): Observable<ItemDetail> {
    return measured(this.metrics, 'graphql', 'items.getById', 1, () =>
      runQuery<{ itemById: RawItemDetail }, ItemDetail>(
        this.apollo,
        ITEM_BY_ID,
        (data) => ({
          summary: GraphQlItemsGateway.toSummary(data.itemById.summary),
          options: data.itemById.options,
          rubricGuidance: data.itemById.rubricGuidance,
          rubricMinimumWords: data.itemById.rubricMinimumWords,
          rubricMaximumWords: data.itemById.rubricMaximumWords,
          sampleAnswer: data.itemById.sampleAnswer,
          versions: data.itemById.versions.map(GraphQlItemsGateway.toVersion),
        }),
        { id },
      ),
    );
  }

  /** @inheritdoc */
  override getVersions(id: string): Observable<readonly ItemVersion[]> {
    return measured(this.metrics, 'graphql', 'items.getVersions', 1, () =>
      runQuery<{ itemVersions: readonly RawItemVersion[] }, readonly ItemVersion[]>(
        this.apollo,
        ITEM_VERSIONS,
        (data) => data.itemVersions.map(GraphQlItemsGateway.toVersion),
        { itemId: id },
      ),
    );
  }

  /** @inheritdoc */
  override create(draft: ItemDraft): Observable<string> {
    return measured(this.metrics, 'graphql', 'items.create', 1, () =>
      runMutation<{ createItem: string }, string>(
        this.apollo,
        CREATE_ITEM,
        (data) => data.createItem,
        { input: GraphQlItemsGateway.toInput(draft) },
      ),
    );
  }

  /** @inheritdoc */
  override update(id: string, draft: ItemDraft): Observable<void> {
    return measured(this.metrics, 'graphql', 'items.update', 1, () =>
      runMutation<{ updateItem: boolean }, void>(this.apollo, UPDATE_ITEM, () => undefined, {
        input: { itemId: id, ...GraphQlItemsGateway.toInput(draft, false) },
      }),
    );
  }

  /** @inheritdoc */
  override remove(id: string): Observable<void> {
    return measured(this.metrics, 'graphql', 'items.delete', 1, () =>
      runMutation<{ deleteItem: boolean }, void>(this.apollo, DELETE_ITEM, () => undefined, {
        itemId: id,
      }),
    );
  }

  /** @inheritdoc */
  override transition(id: string, transition: ItemTransition): Observable<void> {
    return measured(this.metrics, 'graphql', `items.${transition}`, 1, () =>
      runMutation<Record<string, boolean>, void>(
        this.apollo,
        TRANSITION_DOCUMENTS[transition],
        () => undefined,
        { itemId: id },
      ),
    );
  }

  private static toSummary(raw: RawItemSummary): ItemSummary {
    return {
      ...raw,
      type: fromGraphQlEnum<ItemType>(raw.type),
      status: fromGraphQlEnum<ItemStatus>(raw.status),
      difficulty: fromGraphQlEnum<DifficultyLevel>(raw.difficulty),
    };
  }

  private static toVersion(raw: RawItemVersion): ItemVersion {
    return { ...raw, difficulty: fromGraphQlEnum<DifficultyLevel>(raw.difficulty) };
  }

  private static toInput(draft: ItemDraft, includeType = true): Record<string, unknown> {
    const input: Record<string, unknown> = {
      stem: draft.stem,
      difficulty: toGraphQlEnum(draft.difficulty),
      categoryId: draft.categoryId,
      maximumScore: draft.maximumScore,
      options:
        draft.options?.map((option) => ({
          text: option.text,
          isCorrect: option.isCorrect,
          feedback: option.feedback ?? null,
        })) ?? null,
      rubric: draft.rubric ?? null,
      sampleAnswer: draft.sampleAnswer ?? null,
      tagIds: draft.tagIds ? [...draft.tagIds] : [],
    };

    if (includeType) {
      input['type'] = toGraphQlEnum(draft.type);
    }

    return input;
  }
}

/** The GraphQL implementation of {@link TaxonomyGateway}. */
@Injectable({ providedIn: 'root' })
export class GraphQlTaxonomyGateway extends TaxonomyGateway {
  private readonly apollo = inject(Apollo);
  private readonly metrics = inject(MetricsCollector);

  /** @inheritdoc */
  override categories(): Observable<readonly Category[]> {
    return measured(this.metrics, 'graphql', 'taxonomy.categories', 1, () =>
      runQuery<{ categories: readonly Category[] }, readonly Category[]>(
        this.apollo,
        CATEGORIES,
        (data) => data.categories,
      ),
    );
  }

  /** @inheritdoc */
  override tags(): Observable<readonly Tag[]> {
    return measured(this.metrics, 'graphql', 'taxonomy.tags', 1, () =>
      runQuery<{ tags: readonly Tag[] }, readonly Tag[]>(this.apollo, TAGS, (data) => data.tags),
    );
  }

  /** @inheritdoc */
  override createTag(name: string): Observable<string> {
    return measured(this.metrics, 'graphql', 'taxonomy.createTag', 1, () =>
      runMutation<{ createTag: string }, string>(
        this.apollo,
        CREATE_TAG,
        (data) => data.createTag,
        { name },
      ).pipe(map((id) => id)),
    );
  }
}
