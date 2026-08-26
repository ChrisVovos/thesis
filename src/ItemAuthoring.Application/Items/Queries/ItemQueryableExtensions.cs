using ItemAuthoring.Application.Items.Dtos;

namespace ItemAuthoring.Application.Items.Queries;

/// <summary>
/// Translates item search criteria into predicates that Entity Framework Core can push into SQL.
/// </summary>
/// <remarks>
/// This is the single definition of what "filter the item bank" means. The REST query handler calls
/// it, and so does the GraphQL resolver before Hot Chocolate appends its own middleware predicates.
/// Duplicating any of it would mean the two surfaces could return different rows for the same intent,
/// which would invalidate every comparison built on top of them.
/// </remarks>
public static class ItemQueryableExtensions
{
    /// <summary>Applies every supplied filter to the query.</summary>
    /// <param name="query">The query to filter.</param>
    /// <param name="criteria">The criteria supplied by the caller.</param>
    /// <returns>The filtered query.</returns>
    public static IQueryable<ItemSummaryDto> ApplyFilters(
        this IQueryable<ItemSummaryDto> query,
        ItemSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (criteria.Types is { Count: > 0 } types)
        {
            query = query.Where(item => types.Contains(item.Type));
        }

        if (criteria.Statuses is { Count: > 0 } statuses)
        {
            query = query.Where(item => statuses.Contains(item.Status));
        }

        if (criteria.Difficulties is { Count: > 0 } difficulties)
        {
            query = query.Where(item => difficulties.Contains(item.Difficulty));
        }

        if (criteria.CategoryId is { } categoryId)
        {
            query = query.Where(item => item.CategoryId == categoryId);
        }

        if (criteria.AuthorId is { } authorId)
        {
            query = query.Where(item => item.AuthorId == authorId);
        }

        if (criteria.TagIds is { Count: > 0 } tagIds)
        {
            query = query.Where(item => item.Tags.Count(tag => tagIds.Contains(tag.Id)) == tagIds.Count);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(item =>
                item.Stem.Contains(term) || item.CategoryName.Contains(term));
        }

        return query;
    }

    /// <summary>Applies the requested ordering, falling back to a stable default.</summary>
    /// <param name="query">The query to order.</param>
    /// <param name="criteria">The criteria supplied by the caller.</param>
    /// <returns>The ordered query.</returns>
    public static IQueryable<ItemSummaryDto> ApplySorting(
        this IQueryable<ItemSummaryDto> query,
        ItemSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var descending = criteria.SortDescending;
        return (criteria.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "stem" => Order(query, item => item.Stem, descending),
            "difficulty" => Order(query, item => item.Difficulty, descending),
            "status" => Order(query, item => item.Status, descending),
            "type" => Order(query, item => item.Type, descending),
            "category" => Order(query, item => item.CategoryName, descending),
            "score" => Order(query, item => item.MaximumScore, descending),
            "updated" => Order(query, item => item.LastModifiedAtUtc, descending),
            _ => Order(query, item => item.CreatedAtUtc, descending: true),
        };
    }

    private static IQueryable<ItemSummaryDto> Order<TKey>(
        IQueryable<ItemSummaryDto> query,
        System.Linq.Expressions.Expression<Func<ItemSummaryDto, TKey>> selector,
        bool descending)
        => descending
            ? query.OrderByDescending(selector).ThenByDescending(item => item.Id)
            : query.OrderBy(selector).ThenBy(item => item.Id);
}
