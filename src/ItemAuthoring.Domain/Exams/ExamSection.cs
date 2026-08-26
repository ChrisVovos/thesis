using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Domain.Exams;

/// <summary>
/// A titled group of items inside an exam.
/// </summary>
public sealed class ExamSection : Entity<ExamSectionId>
{
    /// <summary>The inclusive maximum length of a section title.</summary>
    public const int MaxTitleLength = 256;

    /// <summary>The inclusive maximum length of the candidate instructions.</summary>
    public const int MaxInstructionsLength = 2000;

    private readonly List<ExamItem> _items = [];

    private ExamSection(ExamSectionId id, ExamId examId, string title, string? instructions, int position)
        : base(id)
    {
        ExamId = examId;
        Title = title;
        Instructions = instructions;
        Position = position;
    }

    private ExamSection()
    {
    }

    /// <summary>Gets the exam the section belongs to.</summary>
    public ExamId ExamId { get; private set; }

    /// <summary>Gets the section title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the optional instructions shown to the candidate.</summary>
    public string? Instructions { get; private set; }

    /// <summary>Gets the zero based position of the section within the exam.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the item placements of the section, in display order.</summary>
    public IReadOnlyCollection<ExamItem> Items => _items.AsReadOnly();

    /// <summary>Creates a section.</summary>
    /// <param name="examId">The exam the section belongs to.</param>
    /// <param name="title">The section title.</param>
    /// <param name="instructions">Optional candidate instructions.</param>
    /// <param name="position">The zero based position within the exam.</param>
    /// <returns>The new section.</returns>
    /// <exception cref="DomainException">The title or instructions were invalid.</exception>
    internal static ExamSection Create(ExamId examId, string? title, string? instructions, int position)
        => new(
            ExamSectionId.New(),
            examId,
            NormalizeTitle(title),
            NormalizeInstructions(instructions),
            position);

    /// <summary>Replaces the editorial details of the section.</summary>
    /// <param name="title">The new title.</param>
    /// <param name="instructions">The new instructions, or <see langword="null"/> to clear them.</param>
    /// <exception cref="DomainException">The title or instructions were invalid.</exception>
    internal void UpdateDetails(string? title, string? instructions)
    {
        Title = NormalizeTitle(title);
        Instructions = NormalizeInstructions(instructions);
    }

    /// <summary>Moves the section to a new position within the exam.</summary>
    /// <param name="position">The new zero based position.</param>
    internal void MoveTo(int position) => Position = position;

    /// <summary>Appends an item to the end of the section.</summary>
    /// <param name="itemId">The referenced bank item.</param>
    /// <param name="scoreOverride">An optional exam specific score.</param>
    /// <returns>The placement.</returns>
    /// <exception cref="DomainException">The item is already present in this section.</exception>
    internal ExamItem AddItem(ItemId itemId, Points? scoreOverride)
    {
        Ensure.That(
            !_items.Exists(item => item.ItemId == itemId),
            "exam.duplicate_item",
            "The item is already present in this section.");

        var placement = ExamItem.Place(Id, itemId, _items.Count, scoreOverride);
        _items.Add(placement);
        return placement;
    }

    /// <summary>Removes an item from the section and closes the gap in the ordering.</summary>
    /// <param name="examItemId">The placement to remove.</param>
    /// <returns><see langword="true"/> when a placement was removed.</returns>
    internal bool RemoveItem(ExamItemId examItemId)
    {
        var removed = _items.RemoveAll(item => item.Id == examItemId) > 0;
        if (removed)
        {
            Reindex();
        }

        return removed;
    }

    /// <summary>Reorders the section so that the placements appear in the supplied sequence.</summary>
    /// <param name="orderedIds">Every placement of the section, in the desired order.</param>
    /// <exception cref="DomainException">The sequence does not describe exactly the current placements.</exception>
    internal void ReorderItems(IReadOnlyList<ExamItemId> orderedIds)
    {
        Ensure.That(
            orderedIds.Count == _items.Count && orderedIds.Distinct().Count() == orderedIds.Count,
            "exam.reorder_incomplete",
            "Reordering must list every item of the section exactly once.");

        var lookup = _items.ToDictionary(item => item.Id);
        var reordered = new List<ExamItem>(orderedIds.Count);
        foreach (var id in orderedIds)
        {
            Ensure.That(
                lookup.ContainsKey(id),
                "exam.reorder_unknown_item",
                "Reordering referenced an item that is not in the section.");
            reordered.Add(lookup[id]);
        }

        _items.Clear();
        _items.AddRange(reordered);
        Reindex();
    }

    /// <summary>Finds a placement by its identity.</summary>
    /// <param name="examItemId">The placement identity.</param>
    /// <returns>The placement, or <see langword="null"/> when it is not in this section.</returns>
    internal ExamItem? FindItem(ExamItemId examItemId)
        => _items.Find(item => item.Id == examItemId);

    private void Reindex()
    {
        for (var position = 0; position < _items.Count; position++)
        {
            _items[position].MoveTo(position);
        }
    }

    private static string NormalizeTitle(string? title)
    {
        var trimmed = Ensure.NotBlank(
            title,
            "exam.section_title_required",
            "A section title is required.");
        return Ensure.MaxLength(
            trimmed,
            MaxTitleLength,
            "exam.section_title_too_long",
            $"A section title must not exceed {MaxTitleLength} characters.");
    }

    private static string? NormalizeInstructions(string? instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return null;
        }

        var trimmed = instructions.Trim();
        return Ensure.MaxLength(
            trimmed,
            MaxInstructionsLength,
            "exam.section_instructions_too_long",
            $"Section instructions must not exceed {MaxInstructionsLength} characters.");
    }
}
