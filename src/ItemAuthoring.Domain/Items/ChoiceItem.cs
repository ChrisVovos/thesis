using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// Base class for every item whose response is a selection from a fixed set of options.
/// </summary>
/// <remarks>
/// The option list is owned here; the rule that decides what a valid option set looks like is
/// deferred to the concrete shape, so adding a new selection-based shape never requires editing an
/// existing one.
/// </remarks>
public abstract class ChoiceItem : Item
{
    private readonly List<ItemOption> _options = [];

    /// <summary>Initializes a new choice item in <see cref="ItemStatus.Draft"/>.</summary>
    /// <param name="type">The answer shape of the item.</param>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a fully correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    protected ChoiceItem(
        ItemType type,
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId)
        : base(type, stem, difficulty, categoryId, maximumScore, authorId)
    {
    }

    /// <summary>Initializes a new choice item for the persistence layer only.</summary>
    protected ChoiceItem()
    {
    }

    /// <summary>Gets the answer options in display order.</summary>
    public IReadOnlyCollection<ItemOption> Options => _options.AsReadOnly();

    /// <summary>Replaces the complete option set of a draft item.</summary>
    /// <param name="options">The options the item should carry afterwards, in display order.</param>
    /// <exception cref="DomainException">The item is not editable or the option set is invalid.</exception>
    public void ReplaceOptions(IEnumerable<ItemOption> options)
    {
        EnsureEditable();
        var ordered = options.ToList();
        EnsureOptionSetIsValid(ordered);

        _options.Clear();
        for (var position = 0; position < ordered.Count; position++)
        {
            var option = ordered[position];
            option.MoveTo(position);
            option.AttachTo(Id);
            _options.Add(option);
        }
    }

    /// <summary>Fails when the supplied option set violates the rules of this answer shape.</summary>
    /// <param name="options">The candidate option set.</param>
    /// <exception cref="DomainException">The option set is invalid.</exception>
    protected abstract void EnsureOptionSetIsValid(IReadOnlyList<ItemOption> options);

    /// <inheritdoc />
    protected override void EnsureContentIsComplete() => EnsureOptionSetIsValid(_options);

    /// <inheritdoc />
    protected override ItemVersionContent CaptureContent()
    {
        var snapshot = _options
            .OrderBy(option => option.Position)
            .Select(option => new ItemVersionOption(
                option.Text.Text,
                option.IsCorrect,
                option.Position,
                option.Feedback))
            .ToList();
        return new ItemVersionContent(snapshot, null);
    }

    /// <summary>Fails when the option set does not contain at least <paramref name="minimum"/> entries.</summary>
    /// <param name="options">The candidate option set.</param>
    /// <param name="minimum">The inclusive minimum number of options.</param>
    /// <exception cref="DomainException">Too few options were supplied.</exception>
    protected static void EnsureMinimumOptions(IReadOnlyList<ItemOption> options, int minimum)
        => Ensure.That(
            options.Count >= minimum,
            "item.too_few_options",
            $"This item type requires at least {minimum} options.");

    /// <summary>Counts the options marked as correct.</summary>
    /// <param name="options">The candidate option set.</param>
    /// <returns>The number of correct options.</returns>
    protected static int CountCorrect(IReadOnlyList<ItemOption> options)
        => options.Count(option => option.IsCorrect);
}
