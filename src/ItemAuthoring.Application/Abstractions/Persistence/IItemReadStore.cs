using ItemAuthoring.Application.Items.Dtos;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// The read side of the item bank.
/// </summary>
/// <remarks>
/// <para>
/// Queries do not go through the write-side repository. Loading an aggregate to render a grid would
/// materialize behaviour-rich objects that the caller only reads, and would make the filtering,
/// sorting and paging happen in memory instead of in SQL Server.
/// </para>
/// <para>
/// <see cref="QuerySummaries"/> returns a composable <see cref="IQueryable{T}"/> over an already
/// projected DTO. Both API surfaces build on it: REST composes filters explicitly, and the GraphQL
/// middleware appends its own predicates. In both cases the predicate reaches the database, which is
/// what makes the payload and latency measurements in the study comparable.
/// </para>
/// </remarks>
public interface IItemReadStore
{
    /// <summary>Opens a composable query over the item bank, excluding deleted items.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<ItemSummaryDto> QuerySummaries();

    /// <summary>Loads the full projection of a single item.</summary>
    /// <param name="itemId">The item to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The item, or <see langword="null"/> when it does not exist.</returns>
    Task<ItemDetailDto?> GetDetailAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Loads several item summaries in one round trip, for batched GraphQL resolution.</summary>
    /// <param name="itemIds">The items to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The summaries, keyed by item identity.</returns>
    Task<IReadOnlyDictionary<Guid, ItemSummaryDto>> GetSummariesAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the answer options of several items in one round trip.</summary>
    /// <param name="itemIds">The items to load options for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The options, keyed by item identity.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemOptionDto>>> GetOptionsAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the published versions of an item, newest first.</summary>
    /// <param name="itemId">The item to load versions for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The published versions.</returns>
    Task<IReadOnlyList<ItemVersionDto>> GetVersionsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The read side of the category and tag taxonomy.
/// </summary>
public interface ITaxonomyReadStore
{
    /// <summary>Opens a composable query over the categories.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<CategoryDto> QueryCategories();

    /// <summary>Opens a composable query over the tags.</summary>
    /// <returns>The composable query.</returns>
    IQueryable<TagDto> QueryTags();

    /// <summary>Loads several categories in one round trip, for batched GraphQL resolution.</summary>
    /// <param name="categoryIds">The categories to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The categories, keyed by identity.</returns>
    Task<IReadOnlyDictionary<Guid, CategoryDto>> GetCategoriesAsync(
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the tags attached to several items in one round trip.</summary>
    /// <param name="itemIds">The items to load tags for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The tags, keyed by item identity.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemTagDto>>> GetTagsByItemAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default);
}
