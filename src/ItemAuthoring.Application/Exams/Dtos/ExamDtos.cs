using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Exams;

namespace ItemAuthoring.Application.Exams.Dtos;

/// <summary>The placement of a bank item inside an exam section.</summary>
public sealed record ExamItemDto
{
    /// <summary>Gets the identity of the placement.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the identity of the referenced bank item.</summary>
    public Guid ItemId { get; init; }

    /// <summary>Gets the zero based position within the section.</summary>
    public int Position { get; init; }

    /// <summary>Gets the exam specific score, when one was set.</summary>
    public decimal? ScoreOverride { get; init; }

    /// <summary>Gets the score the placement contributes to the exam total.</summary>
    public decimal EffectiveScore { get; init; }

    /// <summary>Gets the referenced bank item.</summary>
    public ItemSummaryDto? Item { get; init; }
}

/// <summary>A titled group of items inside an exam.</summary>
public sealed record ExamSectionDto
{
    /// <summary>Gets the identity of the section.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the section title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the optional candidate instructions.</summary>
    public string? Instructions { get; init; }

    /// <summary>Gets the zero based position within the exam.</summary>
    public int Position { get; init; }

    /// <summary>Gets the item placements in display order.</summary>
    public IReadOnlyList<ExamItemDto> Items { get; init; } = [];
}

/// <summary>The list projection of an exam.</summary>
public sealed record ExamSummaryDto
{
    /// <summary>Gets the identity of the exam.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the exam title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the lifecycle status of the exam.</summary>
    public ExamStatus Status { get; init; }

    /// <summary>Gets the delivery time limit in minutes, when one applies.</summary>
    public int? TimeLimitMinutes { get; init; }

    /// <summary>Gets the percentage of the total score required to pass.</summary>
    public int PassingScorePercentage { get; init; }

    /// <summary>Gets the identity of the owning instructor.</summary>
    public Guid OwnerId { get; init; }

    /// <summary>Gets the display name of the owning instructor.</summary>
    public string OwnerName { get; init; } = string.Empty;

    /// <summary>Gets the number of sections in the exam.</summary>
    public int SectionCount { get; init; }

    /// <summary>Gets the number of item placements in the exam.</summary>
    public int ItemCount { get; init; }

    /// <summary>Gets the sum of the scores of every placement.</summary>
    public decimal TotalScore { get; init; }

    /// <summary>Gets the instant, in UTC, at which the exam was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets the instant, in UTC, at which the exam was published.</summary>
    public DateTimeOffset? PublishedAtUtc { get; init; }
}

/// <summary>The full projection of an exam, shaped for the builder and the preview screen.</summary>
public sealed record ExamDetailDto
{
    /// <summary>Gets the list projection of the exam.</summary>
    public ExamSummaryDto Summary { get; init; } = new();

    /// <summary>Gets the sections in display order.</summary>
    public IReadOnlyList<ExamSectionDto> Sections { get; init; } = [];

    /// <summary>Gets the composition rules the exam currently violates.</summary>
    public IReadOnlyList<string> CompositionViolations { get; init; } = [];
}
