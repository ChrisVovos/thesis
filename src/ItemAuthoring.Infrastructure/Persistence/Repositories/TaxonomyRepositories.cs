using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.Repositories;

/// <summary>The Entity Framework Core implementation of <see cref="ICategoryRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    /// <inheritdoc />
    public Task<Category?> GetAsync(CategoryId categoryId, CancellationToken cancellationToken = default)
        => context.Categories.FirstOrDefaultAsync(
            category => category.Id == categoryId,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsActiveAsync(CategoryId categoryId, CancellationToken cancellationToken = default)
        => context.Categories.AnyAsync(
            category => category.Id == categoryId && category.IsActive,
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(
        string name,
        CategoryId? parentId,
        CategoryId? excluding = null,
        CancellationToken cancellationToken = default)
        => context.Categories.AnyAsync(
            category => category.Name.Value == name
                && category.ParentCategoryId == parentId
                && (excluding == null || category.Id != excluding),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasItemsAsync(CategoryId categoryId, CancellationToken cancellationToken = default)
        => context.Items.AnyAsync(item => item.CategoryId == categoryId, cancellationToken);

    /// <inheritdoc />
    public void Add(Category category) => context.Categories.Add(category);

    /// <inheritdoc />
    public void Remove(Category category) => context.Categories.Remove(category);
}

/// <summary>The Entity Framework Core implementation of <see cref="ITagRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class TagRepository(ApplicationDbContext context) : ITagRepository
{
    /// <inheritdoc />
    public Task<Tag?> GetAsync(TagId tagId, CancellationToken cancellationToken = default)
        => context.Tags.FirstOrDefaultAsync(tag => tag.Id == tagId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetByNormalizedNamesAsync(
        IReadOnlyCollection<string> normalizedNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(normalizedNames);
        if (normalizedNames.Count == 0)
        {
            return [];
        }

        var names = normalizedNames.ToList();
        return await context.Tags
            .Where(tag => names.Contains(tag.Name.Normalized))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagId>> FindMissingAsync(
        IReadOnlyCollection<TagId> tagIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        if (tagIds.Count == 0)
        {
            return [];
        }

        var ids = tagIds.Distinct().ToList();
        var known = await context.Tags
            .Where(tag => ids.Contains(tag.Id))
            .Select(tag => tag.Id)
            .ToListAsync(cancellationToken);

        return ids.Except(known).ToList();
    }

    /// <inheritdoc />
    public void Add(Tag tag) => context.Tags.Add(tag);

    /// <inheritdoc />
    public void Remove(Tag tag) => context.Tags.Remove(tag);
}
