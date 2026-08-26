namespace ItemAuthoring.Application.Items.Dtos;

/// <summary>A node in the item bank taxonomy.</summary>
public sealed record CategoryDto
{
    /// <summary>Gets the identity of the category.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the parent category, or <see langword="null"/> for a root category.</summary>
    public Guid? ParentCategoryId { get; init; }

    /// <summary>Gets a value indicating whether new items may be filed under the category.</summary>
    public bool IsActive { get; init; }

    /// <summary>Gets the number of items currently filed under the category.</summary>
    public int ItemCount { get; init; }
}

/// <summary>A free-form label attached to items.</summary>
public sealed record TagDto
{
    /// <summary>Gets the identity of the tag.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the tag label.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the number of items carrying the tag.</summary>
    public int ItemCount { get; init; }
}
