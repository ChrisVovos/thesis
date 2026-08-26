using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Dtos;

/// <summary>An immutable published version of an item.</summary>
public sealed record ItemVersionDto
{
    /// <summary>Gets the identity of the version.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the one based version number.</summary>
    public int VersionNumber { get; init; }

    /// <summary>Gets the instant, in UTC, at which the version was published.</summary>
    public DateTimeOffset PublishedAtUtc { get; init; }

    /// <summary>Gets the prompt as published.</summary>
    public string StemText { get; init; } = string.Empty;

    /// <summary>Gets the cognitive demand as published.</summary>
    public DifficultyLevel Difficulty { get; init; }

    /// <summary>Gets the maximum score as published.</summary>
    public decimal MaximumScore { get; init; }

    /// <summary>Gets the frozen options in display order.</summary>
    public IReadOnlyList<ItemOptionDto> Options { get; init; } = [];
}

/// <summary>The full projection of an item, shaped for the editor and the preview screen.</summary>
public sealed record ItemDetailDto
{
    /// <summary>Gets the list projection of the item.</summary>
    public ItemSummaryDto Summary { get; init; } = new();

    /// <summary>Gets the answer options, empty for essay items.</summary>
    public IReadOnlyList<ItemOptionDto> Options { get; init; } = [];

    /// <summary>Gets the grading guidance of an essay item.</summary>
    public string? RubricGuidance { get; init; }

    /// <summary>Gets the minimum word count of an essay item.</summary>
    public int? RubricMinimumWords { get; init; }

    /// <summary>Gets the maximum word count of an essay item.</summary>
    public int? RubricMaximumWords { get; init; }

    /// <summary>Gets the exemplar answer of an essay item.</summary>
    public string? SampleAnswer { get; init; }

    /// <summary>Gets the published versions of the item, newest first.</summary>
    public IReadOnlyList<ItemVersionDto> Versions { get; init; } = [];
}
