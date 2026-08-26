using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Loads and stores <see cref="Category"/> aggregates.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>Loads a category.</summary>
    /// <param name="categoryId">The category to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The category, or <see langword="null"/> when it does not exist.</returns>
    Task<Category?> GetAsync(CategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a category exists and accepts new items.</summary>
    /// <param name="categoryId">The category to test for.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the category exists and is active.</returns>
    Task<bool> IsActiveAsync(CategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a sibling category already uses a name.</summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="parentId">The parent under which uniqueness is required.</param>
    /// <param name="excluding">A category to ignore, used when renaming.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    Task<bool> NameExistsAsync(
        string name,
        CategoryId? parentId,
        CategoryId? excluding = null,
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether any item is filed under a category.</summary>
    /// <param name="categoryId">The category to test.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the category is in use.</returns>
    Task<bool> HasItemsAsync(CategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>Registers a new category for insertion.</summary>
    /// <param name="category">The category to add.</param>
    void Add(Category category);

    /// <summary>Registers a category for deletion.</summary>
    /// <param name="category">The category to remove.</param>
    void Remove(Category category);
}

/// <summary>
/// Loads and stores <see cref="Tag"/> aggregates.
/// </summary>
public interface ITagRepository
{
    /// <summary>Loads a tag.</summary>
    /// <param name="tagId">The tag to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The tag, or <see langword="null"/> when it does not exist.</returns>
    Task<Tag?> GetAsync(TagId tagId, CancellationToken cancellationToken = default);

    /// <summary>Loads the tags whose normalized names appear in the supplied set.</summary>
    /// <param name="normalizedNames">The normalized names to look up.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The matching tags.</returns>
    Task<IReadOnlyList<Tag>> GetByNormalizedNamesAsync(
        IReadOnlyCollection<string> normalizedNames,
        CancellationToken cancellationToken = default);

    /// <summary>Determines which of the supplied identifiers do not exist.</summary>
    /// <param name="tagIds">The identifiers to verify.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identifiers that were not found.</returns>
    Task<IReadOnlyList<TagId>> FindMissingAsync(
        IReadOnlyCollection<TagId> tagIds,
        CancellationToken cancellationToken = default);

    /// <summary>Registers a new tag for insertion.</summary>
    /// <param name="tag">The tag to add.</param>
    void Add(Tag tag);

    /// <summary>Registers a tag for deletion.</summary>
    /// <param name="tag">The tag to remove.</param>
    void Remove(Tag tag);
}
