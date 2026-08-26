import { emptyPage, toPagedResult } from './paging.models';

describe('paging metadata', () => {
  it('derives the page count and the navigation flags', () => {
    const page = toPagedResult([1, 2, 3], 25, 2, 10);

    expect(page.totalPages).toBe(3);
    expect(page.hasPreviousPage).toBe(true);
    expect(page.hasNextPage).toBe(true);
  });

  it('reports no next page on the last one', () => {
    const page = toPagedResult([1], 21, 3, 10);

    expect(page.totalPages).toBe(3);
    expect(page.hasNextPage).toBe(false);
  });

  it('reports no previous page on the first one', () => {
    expect(toPagedResult([1], 5, 1, 10).hasPreviousPage).toBe(false);
  });

  it('handles an empty result without dividing by zero', () => {
    const page = toPagedResult([], 0, 1, 0);

    expect(page.totalPages).toBe(0);
    expect(page.hasNextPage).toBe(false);
  });

  it('offers a well formed empty page for a screen that has not loaded yet', () => {
    const page = emptyPage<string>(2, 50);

    expect(page.items).toEqual([]);
    expect(page.page).toBe(2);
    expect(page.pageSize).toBe(50);
    expect(page.totalCount).toBe(0);
  });
});
