/** One page of a larger result set, together with the metadata needed to page through it. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

/** The paging, sorting and search parameters shared by every list screen. */
export interface PagedQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly search?: string;
  readonly sortBy?: string;
  readonly sortDescending?: boolean;
}

/** The default page shown before a screen has loaded anything. */
export function emptyPage<T>(page = 1, pageSize = 20): PagedResult<T> {
  return {
    items: [],
    totalCount: 0,
    page,
    pageSize,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

/**
 * Completes the derived paging metadata that only the offset-paged REST payload carries explicitly.
 *
 * @param items The rows on the page.
 * @param totalCount The number of rows matching the query.
 * @param page The one based page index.
 * @param pageSize The page size.
 * @returns The page with its derived metadata filled in.
 */
export function toPagedResult<T>(
  items: readonly T[],
  totalCount: number,
  page: number,
  pageSize: number,
): PagedResult<T> {
  const totalPages = pageSize <= 0 ? 0 : Math.ceil(totalCount / pageSize);
  return {
    items,
    totalCount,
    page,
    pageSize,
    totalPages,
    hasPreviousPage: page > 1,
    hasNextPage: page < totalPages,
  };
}
