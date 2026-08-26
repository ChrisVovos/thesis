using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A frozen copy of a single answer option inside a published item version.
/// </summary>
/// <param name="Text">The option text as published.</param>
/// <param name="IsCorrect">Whether the option scored.</param>
/// <param name="Position">The zero based display position as published.</param>
/// <param name="Feedback">The rationale as published, when one was supplied.</param>
public sealed record ItemVersionOption(string Text, bool IsCorrect, int Position, string? Feedback);

/// <summary>
/// The answer-shape specific content captured when an item is published.
/// </summary>
/// <param name="Options">The frozen options, empty for shapes that have none.</param>
/// <param name="Rubric">The frozen grading rubric, when the shape has one.</param>
public sealed record ItemVersionContent(IReadOnlyList<ItemVersionOption> Options, EssayRubric? Rubric)
{
    /// <summary>Content for an answer shape that has neither options nor a rubric.</summary>
    public static ItemVersionContent Empty { get; } = new([], null);
}

/// <summary>
/// An immutable snapshot of an item at the moment it was published.
/// </summary>
/// <remarks>
/// Once an exam has been assembled from an item, later editorial work must not silently change what
/// candidates saw. Publishing therefore freezes a numbered version that is never updated again.
/// </remarks>
public sealed class ItemVersion : Entity<ItemVersionId>
{
    private readonly List<ItemVersionOption> _options = [];

    private ItemVersion(ItemVersionId id, ItemId itemId, int versionNumber, DateTimeOffset publishedAtUtc)
        : base(id)
    {
        ItemId = itemId;
        VersionNumber = versionNumber;
        PublishedAtUtc = publishedAtUtc;
    }

    private ItemVersion()
    {
    }

    /// <summary>Gets the item this version belongs to.</summary>
    public ItemId ItemId { get; private set; }

    /// <summary>Gets the one based, monotonically increasing version number.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Gets the instant, in UTC, at which the version was published.</summary>
    public DateTimeOffset PublishedAtUtc { get; private set; }

    /// <summary>Gets the answer shape as published.</summary>
    public ItemType Type { get; private set; }

    /// <summary>Gets the prompt as published.</summary>
    public string StemText { get; private set; } = string.Empty;

    /// <summary>Gets the cognitive demand as published.</summary>
    public DifficultyLevel Difficulty { get; private set; }

    /// <summary>Gets the maximum score as published.</summary>
    public decimal MaximumScore { get; private set; }

    /// <summary>Gets the grading guidance as published, when the shape has one.</summary>
    public string? RubricGuidance { get; private set; }

    /// <summary>Gets the minimum word count as published, when the shape has one.</summary>
    public int? RubricMinimumWords { get; private set; }

    /// <summary>Gets the maximum word count as published, when the shape has one.</summary>
    public int? RubricMaximumWords { get; private set; }

    /// <summary>Gets the frozen options, ordered by their published position.</summary>
    public IReadOnlyCollection<ItemVersionOption> Options => _options.AsReadOnly();

    /// <summary>Captures a new version snapshot of the supplied item.</summary>
    /// <param name="item">The item being published.</param>
    /// <param name="versionNumber">The version number to assign.</param>
    /// <param name="content">The answer-shape specific content to freeze.</param>
    /// <param name="publishedAtUtc">The publication instant.</param>
    /// <returns>The immutable snapshot.</returns>
    internal static ItemVersion Capture(
        Item item,
        int versionNumber,
        ItemVersionContent content,
        DateTimeOffset publishedAtUtc)
    {
        var version = new ItemVersion(ItemVersionId.New(), item.Id, versionNumber, publishedAtUtc)
        {
            Type = item.Type,
            StemText = item.Stem.Text,
            Difficulty = item.Difficulty,
            MaximumScore = item.MaximumScore.Value,
            RubricGuidance = content.Rubric?.Guidance,
            RubricMinimumWords = content.Rubric?.MinimumWords,
            RubricMaximumWords = content.Rubric?.MaximumWords,
        };

        version._options.AddRange(content.Options.OrderBy(option => option.Position));
        return version;
    }
}
