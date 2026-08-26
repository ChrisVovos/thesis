using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A node in the item bank taxonomy. Categories may nest, which lets a subject be refined into
/// topics without introducing a second concept.
/// </summary>
public sealed class Category : AggregateRoot<CategoryId>
{
    /// <summary>The inclusive maximum length of a category description.</summary>
    public const int MaxDescriptionLength = 1000;

    private Category(CategoryId id, CategoryName name, string? description, CategoryId? parentId)
        : base(id)
    {
        Name = name;
        Description = description;
        ParentCategoryId = parentId;
        IsActive = true;
    }

    private Category()
    {
    }

    /// <summary>Gets the display name of the category.</summary>
    public CategoryName Name { get; private set; } = null!;

    /// <summary>Gets the optional description of what belongs in the category.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the parent category, or <see langword="null"/> for a root category.</summary>
    public CategoryId? ParentCategoryId { get; private set; }

    /// <summary>Gets a value indicating whether new items may be filed under this category.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Creates a category.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="parentId">The parent category, or <see langword="null"/> for a root.</param>
    /// <returns>The new category.</returns>
    /// <exception cref="DomainException">The name or description were invalid.</exception>
    public static Category Create(CategoryName name, string? description = null, CategoryId? parentId = null)
        => new(CategoryId.New(), name, NormalizeDescription(description), parentId);

    /// <summary>Renames the category.</summary>
    /// <param name="name">The new display name.</param>
    public void Rename(CategoryName name) => Name = name;

    /// <summary>Replaces the description of the category.</summary>
    /// <param name="description">The new description, or <see langword="null"/> to clear it.</param>
    /// <exception cref="DomainException">The description was too long.</exception>
    public void Describe(string? description) => Description = NormalizeDescription(description);

    /// <summary>Re-parents the category.</summary>
    /// <param name="parentId">The new parent, or <see langword="null"/> to make it a root.</param>
    /// <exception cref="DomainException">The category would become its own parent.</exception>
    public void MoveTo(CategoryId? parentId)
    {
        Ensure.That(
            parentId != Id,
            "category.self_parent",
            "A category cannot be its own parent.");
        ParentCategoryId = parentId;
    }

    /// <summary>Allows new items to be filed under this category.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Prevents new items from being filed under this category.</summary>
    public void Deactivate() => IsActive = false;

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        Ensure.MaxLength(
            trimmed,
            MaxDescriptionLength,
            "category.description_too_long",
            $"A category description must not exceed {MaxDescriptionLength} characters.");
        return trimmed;
    }
}
