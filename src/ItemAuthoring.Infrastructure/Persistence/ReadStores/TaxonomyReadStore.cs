using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.ReadStores;

/// <summary>The Entity Framework Core implementation of <see cref="ITaxonomyReadStore"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class TaxonomyReadStore(ApplicationDbContext context) : ITaxonomyReadStore
{
    /// <inheritdoc />
    public IQueryable<CategoryDto> QueryCategories()
        => context.Categories
            .AsNoTracking()
            .Select(category => new CategoryDto
            {
                Id = category.Id.Value,
                Name = category.Name.Value,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId == null
                    ? null
                    : category.ParentCategoryId.Value.Value,
                IsActive = category.IsActive,
                ItemCount = context.Items.Count(item => item.CategoryId == category.Id),
            });

    /// <inheritdoc />
    public IQueryable<TagDto> QueryTags()
        => context.Tags
            .AsNoTracking()
            .Select(tag => new TagDto
            {
                Id = tag.Id.Value,
                Name = tag.Name.Value,
                ItemCount = context.ItemTags.Count(association => association.TagId == tag.Id),
            });

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CategoryDto>> GetCategoriesAsync(
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, CategoryDto>();
        }

        var categories = await QueryCategories()
            .Where(category => categoryIds.Contains(category.Id))
            .ToListAsync(cancellationToken);

        return categories.ToDictionary(category => category.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemTagDto>>> GetTagsByItemAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ItemTagDto>>();
        }

        var ids = itemIds.Select(id => new ItemId(id)).ToList();
        var rows = await context.ItemTags
            .AsNoTracking()
            .Where(association => ids.Contains(association.ItemId))
            .Join(
                context.Tags,
                association => association.TagId,
                tag => tag.Id,
                (association, tag) => new
                {
                    ItemId = association.ItemId.Value,
                    Tag = new ItemTagDto { Id = tag.Id.Value, Name = tag.Name.Value },
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemTagDto>)group
                    .Select(row => row.Tag)
                    .OrderBy(tag => tag.Name, StringComparer.Ordinal)
                    .ToList());
    }
}
