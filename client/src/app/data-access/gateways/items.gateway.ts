import type { Observable } from 'rxjs';
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
import type { PagedResult } from '../../shared/models/paging.models';

/** The item bank contract, expressed purely in domain terms. */
export abstract class ItemsGateway {
  /**
   * Searches, filters, sorts and pages the item bank.
   *
   * @param query The criteria supplied by the screen.
   */
  abstract search(query: ItemQuery): Observable<PagedResult<ItemSummary>>;

  /**
   * Reads a single item in full.
   *
   * @param id The identity of the item.
   */
  abstract getById(id: string): Observable<ItemDetail>;

  /**
   * Reads the published versions of an item, newest first.
   *
   * @param id The identity of the item.
   */
  abstract getVersions(id: string): Observable<readonly ItemVersion[]>;

  /**
   * Creates a draft item.
   *
   * @param draft The item to create.
   * @returns The identity of the new item.
   */
  abstract create(draft: ItemDraft): Observable<string>;

  /**
   * Replaces the content of a draft item.
   *
   * @param id The identity of the item.
   * @param draft The new content.
   */
  abstract update(id: string, draft: ItemDraft): Observable<void>;

  /**
   * Logically removes an item.
   *
   * @param id The identity of the item.
   */
  abstract remove(id: string): Observable<void>;

  /**
   * Requests a lifecycle transition.
   *
   * @param id The identity of the item.
   * @param transition The transition to request.
   */
  abstract transition(id: string, transition: ItemTransition): Observable<void>;
}

/** The taxonomy contract, expressed purely in domain terms. */
export abstract class TaxonomyGateway {
  /** Reads the complete category taxonomy. */
  abstract categories(): Observable<readonly Category[]>;

  /** Reads every tag, ordered by label. */
  abstract tags(): Observable<readonly Tag[]>;

  /**
   * Creates a tag, or returns the existing one when the label is already in use.
   *
   * @param name The tag label.
   * @returns The identity of the tag.
   */
  abstract createTag(name: string): Observable<string>;
}
