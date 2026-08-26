using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Exams.Events;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Domain.Exams;

/// <summary>
/// An assembled examination: an ordered set of sections, each holding an ordered set of references
/// to bank items.
/// </summary>
/// <remarks>
/// Sections and placements are part of the exam aggregate rather than aggregates of their own. Every
/// composition rule — unique items, contiguous ordering, non-empty sections — spans the whole exam,
/// so allowing a section to be modified independently would make those rules unenforceable.
/// </remarks>
public sealed partial class Exam : AggregateRoot<ExamId>, ISoftDeletable
{
    /// <summary>The inclusive maximum length of an exam description.</summary>
    public const int MaxDescriptionLength = 2000;

    private readonly List<ExamSection> _sections = [];

    private Exam(
        ExamId id,
        ExamTitle title,
        string? description,
        int? timeLimitMinutes,
        int passingScorePercentage,
        UserId ownerId)
        : base(id)
    {
        Title = title;
        Description = description;
        TimeLimitMinutes = timeLimitMinutes;
        PassingScorePercentage = passingScorePercentage;
        OwnerId = ownerId;
        Status = ExamStatus.Draft;
    }

    private Exam()
    {
    }

    /// <summary>Gets the exam title.</summary>
    public ExamTitle Title { get; private set; } = null!;

    /// <summary>Gets the optional description of the exam.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the current lifecycle status.</summary>
    public ExamStatus Status { get; private set; }

    /// <summary>Gets the delivery time limit in minutes, when one applies.</summary>
    public int? TimeLimitMinutes { get; private set; }

    /// <summary>Gets the percentage of the total score required to pass.</summary>
    public int PassingScorePercentage { get; private set; }

    /// <summary>Gets the instructor who owns the exam.</summary>
    public UserId OwnerId { get; private set; }

    /// <summary>Gets the instant, in UTC, at which the exam was published.</summary>
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <summary>Gets the sections of the exam, in display order.</summary>
    public IReadOnlyCollection<ExamSection> Sections => _sections.AsReadOnly();

    /// <summary>Creates a draft exam.</summary>
    /// <param name="title">The exam title.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="timeLimitMinutes">An optional delivery time limit in minutes.</param>
    /// <param name="passingScorePercentage">The percentage of the total score required to pass.</param>
    /// <param name="ownerId">The instructor creating the exam.</param>
    /// <returns>The new draft exam.</returns>
    /// <exception cref="DomainException">The description, time limit or passing score were invalid.</exception>
    public static Exam Create(
        ExamTitle title,
        string? description,
        int? timeLimitMinutes,
        int passingScorePercentage,
        UserId ownerId)
    {
        var exam = new Exam(
            ExamId.New(),
            title,
            NormalizeDescription(description),
            ValidateTimeLimit(timeLimitMinutes),
            ValidatePassingScore(passingScorePercentage),
            ownerId);
        exam.SetCreatedBy(ownerId);
        exam.Raise(new ExamCreatedDomainEvent(exam.Id, ownerId));
        return exam;
    }

    /// <summary>Replaces the editorial details of a draft exam.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description, or <see langword="null"/> to clear it.</param>
    /// <param name="timeLimitMinutes">The new time limit, or <see langword="null"/> to remove it.</param>
    /// <param name="passingScorePercentage">The new passing score percentage.</param>
    /// <exception cref="DomainException">The exam is not editable or a value was invalid.</exception>
    public void UpdateDetails(
        ExamTitle title,
        string? description,
        int? timeLimitMinutes,
        int passingScorePercentage)
    {
        EnsureEditable();
        Title = title;
        Description = NormalizeDescription(description);
        TimeLimitMinutes = ValidateTimeLimit(timeLimitMinutes);
        PassingScorePercentage = ValidatePassingScore(passingScorePercentage);
    }

    /// <summary>Computes the total score of the exam from its placements.</summary>
    /// <param name="itemScores">The maximum score of every referenced bank item.</param>
    /// <returns>The total score of the exam.</returns>
    /// <exception cref="DomainException">A referenced item was missing from the lookup.</exception>
    public decimal CalculateTotalScore(IReadOnlyDictionary<ItemId, decimal> itemScores)
    {
        var total = 0m;
        foreach (var placement in _sections.SelectMany(section => section.Items))
        {
            if (placement.ScoreOverride is { } scoreOverride)
            {
                total += scoreOverride.Value;
                continue;
            }

            Ensure.That(
                itemScores.ContainsKey(placement.ItemId),
                "exam.item_score_unknown",
                "The score of a referenced item could not be resolved.");
            total += itemScores[placement.ItemId];
        }

        return total;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        return Ensure.MaxLength(
            trimmed,
            MaxDescriptionLength,
            "exam.description_too_long",
            $"An exam description must not exceed {MaxDescriptionLength} characters.");
    }

    private static int? ValidateTimeLimit(int? timeLimitMinutes)
    {
        if (timeLimitMinutes is null)
        {
            return null;
        }

        Ensure.That(
            timeLimitMinutes is > 0 and <= 24 * 60,
            "exam.time_limit_invalid",
            "An exam time limit must be between 1 and 1440 minutes.");
        return timeLimitMinutes;
    }

    private static int ValidatePassingScore(int passingScorePercentage)
    {
        Ensure.That(
            passingScorePercentage is >= 0 and <= 100,
            "exam.passing_score_invalid",
            "A passing score must be between 0 and 100 percent.");
        return passingScorePercentage;
    }
}
