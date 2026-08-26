namespace ItemAuthoring.Application.Common;

/// <summary>
/// The paging, sorting and free-text parameters shared by every list query.
/// </summary>
/// <remarks>
/// The page size is clamped rather than trusted. An unbounded page size is the simplest denial of
/// service vector a list endpoint can offer, and it would also make the REST/GraphQL payload
/// measurements meaningless.
/// </remarks>
public abstract record PagedQuery
{
    /// <summary>The largest page a client may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The page size applied when the client does not ask for one.</summary>
    public const int DefaultPageSize = 20;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    /// <summary>Gets the one based index of the requested page.</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>Gets the requested page size, clamped to <see cref="MaxPageSize"/>.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    /// <summary>Gets the free-text search term, when one was supplied.</summary>
    public string? Search { get; init; }

    /// <summary>Gets the property to sort by, when one was supplied.</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    public bool SortDescending { get; init; }

    /// <summary>Gets the number of rows to skip for the requested page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
