using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The association between an item and one of its tags.
/// </summary>
/// <remarks>
/// The join is modelled explicitly rather than as a skip navigation because the association is part
/// of the item aggregate: tags are attached and detached through <see cref="Item"/>, never directly.
/// </remarks>
public sealed class ItemTag
{
    private ItemTag(ItemId itemId, TagId tagId)
    {
        ItemId = itemId;
        TagId = tagId;
    }

    private ItemTag()
    {
    }

    /// <summary>Gets the tagged item.</summary>
    public ItemId ItemId { get; private set; }

    /// <summary>Gets the tag attached to the item.</summary>
    public TagId TagId { get; private set; }

    /// <summary>Creates an association between an item and a tag.</summary>
    /// <param name="itemId">The tagged item.</param>
    /// <param name="tagId">The tag being attached.</param>
    /// <returns>The association.</returns>
    /// <exception cref="DomainException">The tag identity was empty.</exception>
    internal static ItemTag Create(ItemId itemId, TagId tagId)
    {
        Ensure.That(tagId.Value != Guid.Empty, "item.tag_invalid", "A tag identity is required.");
        return new ItemTag(itemId, tagId);
    }
}
