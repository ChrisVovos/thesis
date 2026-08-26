using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items.Events;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// An assessment item: the aggregate root of the item bank.
/// </summary>
/// <remarks>
/// <para>
/// The four supported answer shapes are modelled as a class hierarchy rather than as a bag of
/// nullable columns guarded by <c>if (type == ...)</c>, so that each shape owns its own invariants
/// and a new shape can be added without editing existing branches.
/// </para>
/// <para>
/// The hierarchy is persisted table-per-hierarchy. The subtypes differ by a handful of nullable
/// columns, every query in the application is polymorphic ("search all items"), and the benchmark
/// comparison between REST and GraphQL is only meaningful if both surfaces execute the same simple
/// single-table SQL rather than a union over four joined tables.
/// </para>
/// </remarks>
public abstract partial class Item : AggregateRoot<ItemId>, ISoftDeletable
{
    private readonly List<ItemTag> _tags = [];
    private readonly List<ItemVersion> _versions = [];

    /// <summary>Initializes a new item in <see cref="ItemStatus.Draft"/>.</summary>
    /// <param name="type">The answer shape of the item.</param>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a fully correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    protected Item(
        ItemType type,
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId)
        : base(ItemId.New())
    {
        Type = type;
        Stem = stem;
        Difficulty = difficulty;
        CategoryId = categoryId;
        MaximumScore = maximumScore;
        AuthorId = authorId;
        Status = ItemStatus.Draft;
        SetCreatedBy(authorId);
        Raise(new ItemCreatedDomainEvent(Id, type, authorId));
    }

    /// <summary>Initializes a new item for the persistence layer only.</summary>
    protected Item()
    {
    }

    /// <summary>Gets the answer shape of the item; also the persistence discriminator.</summary>
    public ItemType Type { get; private set; }

    /// <summary>Gets the prompt shown to the examinee.</summary>
    public ItemStem Stem { get; private set; } = null!;

    /// <summary>Gets the current lifecycle status.</summary>
    public ItemStatus Status { get; private set; }

    /// <summary>Gets the cognitive demand of the item.</summary>
    public DifficultyLevel Difficulty { get; private set; }

    /// <summary>Gets the category the item is filed under.</summary>
    public CategoryId CategoryId { get; private set; }

    /// <summary>Gets the score a fully correct response is worth.</summary>
    public Points MaximumScore { get; private set; } = null!;

    /// <summary>Gets the author who created the item.</summary>
    public UserId AuthorId { get; private set; }

    /// <summary>Gets the number of the most recently published version, or zero when unpublished.</summary>
    public int VersionNumber { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <summary>Gets the tags attached to the item.</summary>
    public IReadOnlyCollection<ItemTag> Tags => _tags.AsReadOnly();

    /// <summary>Gets the immutable published versions of the item, oldest first.</summary>
    public IReadOnlyCollection<ItemVersion> Versions => _versions.AsReadOnly();

    /// <summary>Replaces the editorial metadata of a draft item.</summary>
    /// <param name="stem">The new prompt.</param>
    /// <param name="difficulty">The new cognitive demand.</param>
    /// <param name="categoryId">The new category.</param>
    /// <param name="maximumScore">The new maximum score.</param>
    /// <exception cref="DomainException">The item is not editable in its current status.</exception>
    public void UpdateDetails(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore)
    {
        EnsureEditable();
        Stem = stem;
        Difficulty = difficulty;
        CategoryId = categoryId;
        MaximumScore = maximumScore;
    }

    /// <summary>Attaches a tag to the item, ignoring duplicates.</summary>
    /// <param name="tagId">The tag to attach.</param>
    /// <exception cref="DomainException">The item is not editable in its current status.</exception>
    public void AddTag(TagId tagId)
    {
        EnsureEditable();
        if (_tags.Exists(tag => tag.TagId == tagId))
        {
            return;
        }

        _tags.Add(ItemTag.Create(Id, tagId));
    }

    /// <summary>Detaches a tag from the item, ignoring tags that were never attached.</summary>
    /// <param name="tagId">The tag to detach.</param>
    /// <exception cref="DomainException">The item is not editable in its current status.</exception>
    public void RemoveTag(TagId tagId)
    {
        EnsureEditable();
        _tags.RemoveAll(tag => tag.TagId == tagId);
    }

    /// <summary>Replaces the complete tag set of the item.</summary>
    /// <param name="tagIds">The tags the item should carry afterwards.</param>
    /// <exception cref="DomainException">The item is not editable in its current status.</exception>
    public void ReplaceTags(IEnumerable<TagId> tagIds)
    {
        EnsureEditable();
        _tags.Clear();
        foreach (var tagId in tagIds.Distinct())
        {
            _tags.Add(ItemTag.Create(Id, tagId));
        }
    }

    /// <summary>Fails when the item may not currently be edited.</summary>
    /// <exception cref="DomainException">The item is deleted or no longer a draft.</exception>
    protected void EnsureEditable()
    {
        Ensure.That(!IsDeleted, "item.deleted", "A deleted item cannot be modified.");
        Ensure.That(
            Status is ItemStatus.Draft,
            "item.not_editable",
            $"An item in status '{Status}' cannot be edited; return it to draft first.");
    }

    /// <summary>Fails when the concrete answer shape is not yet complete enough to be reviewed.</summary>
    /// <exception cref="DomainException">The item content is incomplete.</exception>
    protected abstract void EnsureContentIsComplete();

    /// <summary>Captures the answer-shape specific content for an immutable version snapshot.</summary>
    /// <returns>The captured content.</returns>
    protected abstract ItemVersionContent CaptureContent();
}
