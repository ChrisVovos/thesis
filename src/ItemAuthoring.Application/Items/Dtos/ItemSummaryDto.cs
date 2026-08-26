using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Dtos;

/// <summary>A single answer option as presented to a client.</summary>
public sealed record ItemOptionDto
{
    /// <summary>Gets the identity of the option.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the option text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether selecting the option scores.</summary>
    public bool IsCorrect { get; init; }

    /// <summary>Gets the zero based display position.</summary>
    public int Position { get; init; }

    /// <summary>Gets the rationale shown after answering, when one was authored.</summary>
    public string? Feedback { get; init; }
}

/// <summary>A tag attached to an item.</summary>
public sealed record ItemTagDto
{
    /// <summary>Gets the identity of the tag.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the tag label.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>The list projection of an item, shaped for the item bank grid.</summary>
public sealed record ItemSummaryDto
{
    /// <summary>Gets the identity of the item.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the answer shape of the item.</summary>
    public ItemType Type { get; init; }

    /// <summary>Gets the lifecycle status of the item.</summary>
    public ItemStatus Status { get; init; }

    /// <summary>Gets the cognitive demand of the item.</summary>
    public DifficultyLevel Difficulty { get; init; }

    /// <summary>Gets the prompt shown to the examinee.</summary>
    public string Stem { get; init; } = string.Empty;

    /// <summary>Gets the score a fully correct response is worth.</summary>
    public decimal MaximumScore { get; init; }

    /// <summary>Gets the identity of the category the item is filed under.</summary>
    public Guid CategoryId { get; init; }

    /// <summary>Gets the display name of the category the item is filed under.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Gets the identity of the author.</summary>
    public Guid AuthorId { get; init; }

    /// <summary>Gets the display name of the author.</summary>
    public string AuthorName { get; init; } = string.Empty;

    /// <summary>Gets the number of the most recently published version, or zero.</summary>
    public int VersionNumber { get; init; }

    /// <summary>Gets the instant, in UTC, at which the item was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets the instant, in UTC, of the most recent change.</summary>
    public DateTimeOffset? LastModifiedAtUtc { get; init; }

    /// <summary>Gets the tags attached to the item.</summary>
    public IReadOnlyList<ItemTagDto> Tags { get; init; } = [];
}
