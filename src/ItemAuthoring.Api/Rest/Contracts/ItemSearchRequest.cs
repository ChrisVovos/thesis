using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Queries;
using ItemAuthoring.Domain.Items;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Contracts;

/// <summary>
/// The query string of the item search endpoint.
/// </summary>
/// <remarks>
/// A dedicated binding type is used rather than the application query object because the two have
/// genuinely different shapes: a query string carries comma separated scalars, while the application
/// speaks in typed collections. Translating between them is the controller's job.
/// </remarks>
public sealed record ItemSearchRequest
{
    /// <summary>Gets the one based page index.</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Gets the page size, clamped by the application layer.</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = PagedQuery.DefaultPageSize;

    /// <summary>Gets the free-text search term.</summary>
    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    /// <summary>Gets the property to sort by.</summary>
    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    [FromQuery(Name = "sortDescending")]
    public bool SortDescending { get; init; }

    /// <summary>Gets the answer shapes to include.</summary>
    [FromQuery(Name = "type")]
    public ItemType[]? Types { get; init; }

    /// <summary>Gets the lifecycle statuses to include.</summary>
    [FromQuery(Name = "status")]
    public ItemStatus[]? Statuses { get; init; }

    /// <summary>Gets the difficulty levels to include.</summary>
    [FromQuery(Name = "difficulty")]
    public DifficultyLevel[]? Difficulties { get; init; }

    /// <summary>Gets the category to restrict the search to.</summary>
    [FromQuery(Name = "categoryId")]
    public Guid? CategoryId { get; init; }

    /// <summary>Gets the tags an item must carry to be included.</summary>
    [FromQuery(Name = "tagId")]
    public Guid[]? TagIds { get; init; }

    /// <summary>Gets the author to restrict the search to.</summary>
    [FromQuery(Name = "authorId")]
    public Guid? AuthorId { get; init; }

    /// <summary>Converts the query string into the application search criteria.</summary>
    /// <returns>The application search criteria.</returns>
    public ItemSearchCriteria ToCriteria() => new()
    {
        Page = Page,
        PageSize = PageSize,
        Search = Search,
        SortBy = SortBy,
        SortDescending = SortDescending,
        Types = Types,
        Statuses = Statuses,
        Difficulties = Difficulties,
        CategoryId = CategoryId,
        TagIds = TagIds,
        AuthorId = AuthorId,
    };
}
