using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A binary item such as true/false, yes/no or agree/disagree.
/// </summary>
/// <remarks>
/// Either/or is deliberately modelled as a constrained choice item rather than as a boolean flag:
/// the two labels are authored content ("True"/"False" is only one of many valid pairs), and reusing
/// the option machinery means preview, scoring and versioning need no special case for this shape.
/// </remarks>
public sealed class EitherOrItem : ChoiceItem
{
    /// <summary>The exact number of options an either/or item offers.</summary>
    public const int RequiredOptions = 2;

    private EitherOrItem(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId)
        : base(ItemType.EitherOr, stem, difficulty, categoryId, maximumScore, authorId)
    {
    }

    private EitherOrItem()
    {
    }

    /// <summary>Creates a draft either/or item.</summary>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    /// <param name="positiveLabel">The label of the first alternative.</param>
    /// <param name="negativeLabel">The label of the second alternative.</param>
    /// <param name="positiveIsCorrect">Whether the first alternative is the correct one.</param>
    /// <returns>The new draft item.</returns>
    /// <exception cref="DomainException">A label was blank.</exception>
    public static EitherOrItem Create(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        string? positiveLabel,
        string? negativeLabel,
        bool positiveIsCorrect)
    {
        var item = new EitherOrItem(stem, difficulty, categoryId, maximumScore, authorId);
        item.ReplaceOptions(
        [
            ItemOption.Create(positiveLabel, positiveIsCorrect, 0),
            ItemOption.Create(negativeLabel, !positiveIsCorrect, 1),
        ]);
        return item;
    }

    /// <inheritdoc />
    protected override void EnsureOptionSetIsValid(IReadOnlyList<ItemOption> options)
    {
        Ensure.That(
            options.Count == RequiredOptions,
            "item.either_or_requires_two_options",
            "An either/or item must have exactly two options.");
        Ensure.That(
            CountCorrect(options) == 1,
            "item.either_or_requires_one_correct",
            "An either/or item must have exactly one correct option.");
    }
}
