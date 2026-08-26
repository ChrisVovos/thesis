using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Exams.Events;

namespace ItemAuthoring.Domain.Exams;

/// <content>
/// Lifecycle transitions and composition validation.
/// </content>
public sealed partial class Exam
{
    /// <summary>The inclusive minimum number of sections a publishable exam must have.</summary>
    public const int MinimumSections = 1;

    /// <summary>Reports every composition rule the exam currently violates.</summary>
    /// <returns>
    /// The violated rule codes, empty when the exam may be published. The same codes are surfaced by
    /// REST and by GraphQL, so a client can render one set of messages regardless of transport.
    /// </returns>
    public IReadOnlyList<string> ValidateComposition()
    {
        var violations = new List<string>();

        if (_sections.Count < MinimumSections)
        {
            violations.Add("exam.no_sections");
        }

        if (_sections.Exists(section => section.Items.Count == 0))
        {
            violations.Add("exam.empty_section");
        }

        var placements = _sections.SelectMany(section => section.Items).ToList();
        if (placements.Select(item => item.ItemId).Distinct().Count() != placements.Count)
        {
            violations.Add("exam.duplicate_item");
        }

        return violations;
    }

    /// <summary>Freezes the exam for delivery.</summary>
    /// <param name="publishedAtUtc">The publication instant.</param>
    /// <exception cref="DomainException">The exam is not a valid draft.</exception>
    public void Publish(DateTimeOffset publishedAtUtc)
    {
        EnsureEditable();
        var violations = ValidateComposition();
        Ensure.That(
            violations.Count == 0,
            violations.Count > 0 ? violations[0] : "exam.invalid_composition",
            "The exam composition is not valid for publication.");

        Status = ExamStatus.Published;
        PublishedAtUtc = publishedAtUtc;
        Raise(new ExamPublishedDomainEvent(Id, publishedAtUtc));
    }

    /// <summary>Withdraws a published exam from delivery.</summary>
    /// <exception cref="DomainException">The exam is not published.</exception>
    public void Archive()
    {
        Ensure.That(
            Status is ExamStatus.Published,
            "exam.invalid_transition",
            "Only a published exam can be archived.");
        Status = ExamStatus.Archived;
    }

    /// <summary>Returns a published or archived exam to draft so its composition can change again.</summary>
    /// <exception cref="DomainException">The exam has been deleted.</exception>
    public void ReturnToDraft()
    {
        Ensure.That(!IsDeleted, "exam.deleted", "A deleted exam cannot be modified.");
        Status = ExamStatus.Draft;
        PublishedAtUtc = null;
    }

    /// <summary>Logically removes the exam.</summary>
    /// <param name="deletedAtUtc">The deletion instant.</param>
    /// <exception cref="DomainException">The exam is published and must be archived first.</exception>
    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        Ensure.That(
            Status is not ExamStatus.Published,
            "exam.delete_published",
            "A published exam must be archived before it can be deleted.");

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
    }

    /// <summary>Reverses a logical deletion.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }

    private void EnsureEditable()
    {
        Ensure.That(!IsDeleted, "exam.deleted", "A deleted exam cannot be modified.");
        Ensure.That(
            Status is ExamStatus.Draft,
            "exam.not_editable",
            $"An exam in status '{Status}' cannot be changed; return it to draft first.");
    }
}
