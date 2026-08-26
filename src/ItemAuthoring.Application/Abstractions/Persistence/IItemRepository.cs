using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Loads and stores <see cref="Item"/> aggregates.
/// </summary>
/// <remarks>
/// The write side is a repository over aggregates, never over rows: a caller can only obtain a whole
/// item, and therefore can only change it through the methods that enforce its invariants. The read
/// side deliberately does not go through here — see <see cref="IItemReadStore"/>.
/// </remarks>
public interface IItemRepository
{
    /// <summary>Loads an item with the data required to change it.</summary>
    /// <param name="itemId">The item to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The item, or <see langword="null"/> when it does not exist or was deleted.</returns>
    Task<Item?> GetAsync(ItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether an item exists and has not been deleted.</summary>
    /// <param name="itemId">The item to test for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the item exists.</returns>
    Task<bool> ExistsAsync(ItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>Reads the maximum score of several items in one round trip.</summary>
    /// <param name="itemIds">The items to read.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The maximum score of every item that exists.</returns>
    Task<IReadOnlyDictionary<ItemId, decimal>> GetMaximumScoresAsync(
        IReadOnlyCollection<ItemId> itemIds,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the lifecycle status of an item without loading the aggregate.</summary>
    /// <param name="itemId">The item to inspect.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The status, or <see langword="null"/> when the item does not exist.</returns>
    Task<ItemStatus?> GetStatusAsync(ItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>Registers a new item for insertion.</summary>
    /// <param name="item">The item to add.</param>
    void Add(Item item);
}
