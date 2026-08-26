using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A multiple choice item with more than one correct option.
/// </summary>
public sealed class MultipleResponseItem : ChoiceItem
{
    /// <summary>The inclusive minimum number of options a multiple response item must offer.</summary>
    public const int MinimumOptions = 3;

    private MultipleResponseItem(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId)
        : base(
            ItemType.MultipleChoiceMultipleResponse,
            stem,
            difficulty,
            categoryId,
            maximumScore,
            authorId)
    {
    }

    private MultipleResponseItem()
    {
    }

    /// <summary>Creates a draft multiple response item.</summary>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a fully correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    /// <param name="options">The answer options in display order.</param>
    /// <returns>The new draft item.</returns>
    /// <exception cref="DomainException">The option set is invalid.</exception>
    public static MultipleResponseItem Create(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        IEnumerable<ItemOption> options)
    {
        var item = new MultipleResponseItem(stem, difficulty, categoryId, maximumScore, authorId);
        item.ReplaceOptions(options);
        return item;
    }

    /// <inheritdoc />
    protected override void EnsureOptionSetIsValid(IReadOnlyList<ItemOption> options)
    {
        EnsureMinimumOptions(options, MinimumOptions);
        var correct = CountCorrect(options);
        Ensure.That(
            correct >= 2,
            "item.multiple_response_requires_two_correct",
            "A multiple response item must have at least two correct options.");
        Ensure.That(
            correct < options.Count,
            "item.multiple_response_requires_distractor",
            "A multiple response item must have at least one incorrect option.");
    }
}
