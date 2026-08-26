using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A free-form label that cuts across the category taxonomy.
/// </summary>
public sealed class Tag : AggregateRoot<TagId>
{
    private Tag(TagId id, TagName name)
        : base(id) => Name = name;

    private Tag()
    {
    }

    /// <summary>Gets the tag label.</summary>
    public TagName Name { get; private set; } = null!;

    /// <summary>Creates a tag.</summary>
    /// <param name="name">The tag label.</param>
    /// <returns>The new tag.</returns>
    public static Tag Create(TagName name) => new(TagId.New(), name);

    /// <summary>Renames the tag.</summary>
    /// <param name="name">The new tag label.</param>
    public void Rename(TagName name) => Name = name;
}
