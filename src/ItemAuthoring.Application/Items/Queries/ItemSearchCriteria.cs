using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Queries;

/// <summary>Searches, filters, sorts and pages the item bank.</summary>
public sealed record ItemSearchCriteria : PagedQuery
{
    /// <summary>Gets the answer shapes to include; all shapes when empty.</summary>
    public IReadOnlyList<ItemType>? Types { get; init; }

    /// <summary>Gets the lifecycle statuses to include; all statuses when empty.</summary>
    public IReadOnlyList<ItemStatus>? Statuses { get; init; }

    /// <summary>Gets the difficulty levels to include; all levels when empty.</summary>
    public IReadOnlyList<DifficultyLevel>? Difficulties { get; init; }

    /// <summary>Gets the category to restrict the search to.</summary>
    public Guid? CategoryId { get; init; }

    /// <summary>Gets the tags an item must carry to be included.</summary>
    public IReadOnlyList<Guid>? TagIds { get; init; }

    /// <summary>Gets the author to restrict the search to.</summary>
    public Guid? AuthorId { get; init; }
}
