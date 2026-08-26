using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.Repositories;

/// <summary>
/// The Entity Framework Core implementation of <see cref="IItemRepository"/>.
/// </summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class ItemRepository(ApplicationDbContext context) : IItemRepository
{
    /// <inheritdoc />
    public Task<Item?> GetAsync(ItemId itemId, CancellationToken cancellationToken = default)
        => context.Items
            .Include(item => item.Tags)
            .Include(item => item.Versions)
            .Include(item => (item as ChoiceItem)!.Options)
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(ItemId itemId, CancellationToken cancellationToken = default)
        => context.Items.AnyAsync(item => item.Id == itemId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ItemId, decimal>> GetMaximumScoresAsync(
        IReadOnlyCollection<ItemId> itemIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0)
        {
            return new Dictionary<ItemId, decimal>();
        }

        var ids = itemIds.ToList();
        var rows = await context.Items
            .Where(item => ids.Contains(item.Id))
            .Select(item => new { item.Id, item.MaximumScore })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.MaximumScore.Value);
    }

    /// <inheritdoc />
    public async Task<ItemStatus?> GetStatusAsync(
        ItemId itemId,
        CancellationToken cancellationToken = default)
    {
        var statuses = await context.Items
            .Where(item => item.Id == itemId)
            .Select(item => (ItemStatus?)item.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return statuses;
    }

    /// <inheritdoc />
    public void Add(Item item) => context.Items.Add(item);
}
