using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items;

/// <summary>
/// Builds the concrete item aggregate that matches a requested answer shape.
/// </summary>
/// <remarks>
/// A wire format is a tagged union, so translating it into a class hierarchy requires exactly one
/// switch. Confining that switch here means no handler, controller or GraphQL resolver ever has to
/// know that four shapes exist, and adding a fifth touches this file and the domain only.
/// </remarks>
public static class ItemFactory
{
    /// <summary>Creates the draft item described by the supplied shape.</summary>
    /// <param name="type">The answer shape to create.</param>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a fully correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    /// <param name="options">The answer options, required for every shape except essay.</param>
    /// <param name="rubric">The grading guidance, required for essay items.</param>
    /// <param name="sampleAnswer">An optional exemplar answer for essay items.</param>
    /// <returns>The new draft item.</returns>
    /// <exception cref="DomainException">The supplied content does not fit the requested shape.</exception>
    public static Item Create(
        ItemType type,
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        IReadOnlyList<ItemOption> options,
        EssayRubric? rubric,
        string? sampleAnswer)
        => type switch
        {
            ItemType.MultipleChoiceSingleResponse => SingleResponseItem.Create(
                stem, difficulty, categoryId, maximumScore, authorId, options),
            ItemType.MultipleChoiceMultipleResponse => MultipleResponseItem.Create(
                stem, difficulty, categoryId, maximumScore, authorId, options),
            ItemType.EitherOr => CreateEitherOr(
                stem, difficulty, categoryId, maximumScore, authorId, options),
            ItemType.Essay => EssayItem.Create(
                stem,
                difficulty,
                categoryId,
                maximumScore,
                authorId,
                Ensure.NotNull(rubric, "item.rubric_required", "An essay item requires a rubric."),
                sampleAnswer),
            _ => throw new DomainException("item.unknown_type", "The item type is not supported."),
        };

    private static EitherOrItem CreateEitherOr(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        IReadOnlyList<ItemOption> options)
    {
        Ensure.That(
            options.Count == EitherOrItem.RequiredOptions,
            "item.either_or_requires_two_options",
            "An either/or item must have exactly two options.");

        return EitherOrItem.Create(
            stem,
            difficulty,
            categoryId,
            maximumScore,
            authorId,
            options[0].Text.Text,
            options[1].Text.Text,
            options[0].IsCorrect);
    }
}
