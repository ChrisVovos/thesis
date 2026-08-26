namespace ItemAuthoring.Application.Common;

/// <summary>
/// One page of a larger result set, together with the metadata a client needs to page through it.
/// </summary>
/// <typeparam name="T">The type of the items on the page.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">The number of items matching the query across all pages.</param>
/// <param name="Page">The one based index of the current page.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Gets the number of pages the result set spans.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets a value indicating whether a further page exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Creates an empty page.</summary>
    /// <param name="page">The one based index of the requested page.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <returns>The empty page.</returns>
    public static PagedResult<T> Empty(int page, int pageSize) => new([], 0, page, pageSize);
}
