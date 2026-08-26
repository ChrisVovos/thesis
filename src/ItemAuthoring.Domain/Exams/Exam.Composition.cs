using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Domain.Exams;

/// <content>
/// Composition of the exam: sections, item placements and their ordering.
/// </content>
public sealed partial class Exam
{
    /// <summary>Appends a section to the end of the exam.</summary>
    /// <param name="title">The section title.</param>
    /// <param name="instructions">Optional candidate instructions.</param>
    /// <returns>The new section.</returns>
    /// <exception cref="DomainException">The exam is not editable or the title was invalid.</exception>
    public ExamSection AddSection(string? title, string? instructions = null)
    {
        EnsureEditable();
        var section = ExamSection.Create(Id, title, instructions, _sections.Count);
        _sections.Add(section);
        return section;
    }

    /// <summary>Replaces the editorial details of a section.</summary>
    /// <param name="sectionId">The section to change.</param>
    /// <param name="title">The new title.</param>
    /// <param name="instructions">The new instructions, or <see langword="null"/> to clear them.</param>
    /// <exception cref="DomainException">The exam is not editable or the section is unknown.</exception>
    public void UpdateSection(ExamSectionId sectionId, string? title, string? instructions)
    {
        EnsureEditable();
        RequireSection(sectionId).UpdateDetails(title, instructions);
    }

    /// <summary>Removes a section together with all of its placements.</summary>
    /// <param name="sectionId">The section to remove.</param>
    /// <exception cref="DomainException">The exam is not editable or the section is unknown.</exception>
    public void RemoveSection(ExamSectionId sectionId)
    {
        EnsureEditable();
        var removed = _sections.RemoveAll(section => section.Id == sectionId) > 0;
        Ensure.That(removed, "exam.section_not_found", "The section is not part of this exam.");
        ReindexSections();
    }

    /// <summary>Reorders the sections of the exam.</summary>
    /// <param name="orderedIds">Every section of the exam, in the desired order.</param>
    /// <exception cref="DomainException">The exam is not editable or the sequence is incomplete.</exception>
    public void ReorderSections(IReadOnlyList<ExamSectionId> orderedIds)
    {
        EnsureEditable();
        Ensure.That(
            orderedIds.Count == _sections.Count && orderedIds.Distinct().Count() == orderedIds.Count,
            "exam.reorder_incomplete",
            "Reordering must list every section of the exam exactly once.");

        var lookup = _sections.ToDictionary(section => section.Id);
        var reordered = new List<ExamSection>(orderedIds.Count);
        foreach (var id in orderedIds)
        {
            Ensure.That(
                lookup.ContainsKey(id),
                "exam.section_not_found",
                "Reordering referenced a section that is not part of this exam.");
            reordered.Add(lookup[id]);
        }

        _sections.Clear();
        _sections.AddRange(reordered);
        ReindexSections();
    }

    /// <summary>Appends an existing bank item to a section.</summary>
    /// <param name="sectionId">The section to append to.</param>
    /// <param name="itemId">The referenced bank item.</param>
    /// <param name="scoreOverride">An optional exam specific score.</param>
    /// <returns>The new placement.</returns>
    /// <exception cref="DomainException">
    /// The exam is not editable, the section is unknown, or the item already appears in the exam.
    /// </exception>
    public ExamItem AddItem(ExamSectionId sectionId, ItemId itemId, Points? scoreOverride = null)
    {
        EnsureEditable();
        Ensure.That(
            !ContainsItem(itemId),
            "exam.duplicate_item",
            "The item already appears in this exam.");
        return RequireSection(sectionId).AddItem(itemId, scoreOverride);
    }

    /// <summary>Removes a placement from a section.</summary>
    /// <param name="sectionId">The section holding the placement.</param>
    /// <param name="examItemId">The placement to remove.</param>
    /// <exception cref="DomainException">The exam is not editable or the placement is unknown.</exception>
    public void RemoveItem(ExamSectionId sectionId, ExamItemId examItemId)
    {
        EnsureEditable();
        var removed = RequireSection(sectionId).RemoveItem(examItemId);
        Ensure.That(removed, "exam.item_not_found", "The item is not part of this section.");
    }

    /// <summary>Reorders the placements inside a section.</summary>
    /// <param name="sectionId">The section to reorder.</param>
    /// <param name="orderedIds">Every placement of the section, in the desired order.</param>
    /// <exception cref="DomainException">The exam is not editable or the sequence is incomplete.</exception>
    public void ReorderItems(ExamSectionId sectionId, IReadOnlyList<ExamItemId> orderedIds)
    {
        EnsureEditable();
        RequireSection(sectionId).ReorderItems(orderedIds);
    }

    /// <summary>Overrides how much a placement is worth inside this exam.</summary>
    /// <param name="sectionId">The section holding the placement.</param>
    /// <param name="examItemId">The placement to change.</param>
    /// <param name="scoreOverride">The new score, or <see langword="null"/> to fall back to the item.</param>
    /// <exception cref="DomainException">The exam is not editable or the placement is unknown.</exception>
    public void OverrideItemScore(ExamSectionId sectionId, ExamItemId examItemId, Points? scoreOverride)
    {
        EnsureEditable();
        var placement = RequireSection(sectionId).FindItem(examItemId);
        Ensure.NotNull(placement, "exam.item_not_found", "The item is not part of this section.")
            .OverrideScore(scoreOverride);
    }

    /// <summary>Determines whether a bank item already appears anywhere in the exam.</summary>
    /// <param name="itemId">The bank item to look for.</param>
    /// <returns><see langword="true"/> when the item is already placed.</returns>
    public bool ContainsItem(ItemId itemId)
        => _sections.Exists(section => section.Items.Any(item => item.ItemId == itemId));

    private ExamSection RequireSection(ExamSectionId sectionId)
        => Ensure.NotNull(
            _sections.Find(section => section.Id == sectionId),
            "exam.section_not_found",
            "The section is not part of this exam.");

    private void ReindexSections()
    {
        for (var position = 0; position < _sections.Count; position++)
        {
            _sections[position].MoveTo(position);
        }
    }
}
