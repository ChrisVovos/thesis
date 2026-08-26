using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Domain.Exams;

/// <summary>
/// The placement of a single bank item inside an exam section.
/// </summary>
/// <remarks>
/// The exam references the item rather than copying it, and may override how much the item is worth
/// in this exam without changing the item itself.
/// </remarks>
public sealed class ExamItem : Entity<ExamItemId>
{
    private ExamItem(ExamItemId id, ExamSectionId sectionId, ItemId itemId, int position, Points? scoreOverride)
        : base(id)
    {
        ExamSectionId = sectionId;
        ItemId = itemId;
        Position = position;
        ScoreOverride = scoreOverride;
    }

    private ExamItem()
    {
    }

    /// <summary>Gets the section the item is placed in.</summary>
    public ExamSectionId ExamSectionId { get; private set; }

    /// <summary>Gets the referenced bank item.</summary>
    public ItemId ItemId { get; private set; }

    /// <summary>Gets the zero based position within the section.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the exam specific score, or <see langword="null"/> to use the item's own score.</summary>
    public Points? ScoreOverride { get; private set; }

    /// <summary>Places an item in a section.</summary>
    /// <param name="sectionId">The section the item is placed in.</param>
    /// <param name="itemId">The referenced bank item.</param>
    /// <param name="position">The zero based position within the section.</param>
    /// <param name="scoreOverride">An optional exam specific score.</param>
    /// <returns>The placement.</returns>
    /// <exception cref="DomainException">The position was negative.</exception>
    internal static ExamItem Place(
        ExamSectionId sectionId,
        ItemId itemId,
        int position,
        Points? scoreOverride)
    {
        Ensure.That(
            position >= 0,
            "exam.item_position_negative",
            "An exam item position cannot be negative.");
        return new ExamItem(ExamItemId.New(), sectionId, itemId, position, scoreOverride);
    }

    /// <summary>Moves the placement to a new position within its section.</summary>
    /// <param name="position">The new zero based position.</param>
    internal void MoveTo(int position) => Position = position;

    /// <summary>Replaces the exam specific score of the placement.</summary>
    /// <param name="scoreOverride">The new score, or <see langword="null"/> to fall back to the item.</param>
    internal void OverrideScore(Points? scoreOverride) => ScoreOverride = scoreOverride;
}
