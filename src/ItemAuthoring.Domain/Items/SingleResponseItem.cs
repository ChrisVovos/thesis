using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A multiple choice item with exactly one correct option.
/// </summary>
public sealed class SingleResponseItem : ChoiceItem
{
    /// <summary>The inclusive minimum number of options a single response item must offer.</summary>
    public const int MinimumOptions = 2;

    private SingleResponseItem(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId)
        : base(
            ItemType.MultipleChoiceSingleResponse,
            stem,
            difficulty,
            categoryId,
            maximumScore,
            authorId)
    {
    }

    private SingleResponseItem()
    {
    }

    /// <summary>Creates a draft single response item.</summary>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    /// <param name="options">The answer options in display order.</param>
    /// <returns>The new draft item.</returns>
    /// <exception cref="DomainException">The option set is invalid.</exception>
    public static SingleResponseItem Create(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        IEnumerable<ItemOption> options)
    {
        var item = new SingleResponseItem(stem, difficulty, categoryId, maximumScore, authorId);
        item.ReplaceOptions(options);
        return item;
    }

    /// <inheritdoc />
    protected override void EnsureOptionSetIsValid(IReadOnlyList<ItemOption> options)
    {
        EnsureMinimumOptions(options, MinimumOptions);
        Ensure.That(
            CountCorrect(options) == 1,
            "item.single_response_requires_one_correct",
            "A single response item must have exactly one correct option.");
    }
}
