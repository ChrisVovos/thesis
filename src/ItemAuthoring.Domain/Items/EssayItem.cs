using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A free text item graded by a human against a rubric.
/// </summary>
public sealed class EssayItem : Item
{
    /// <summary>The inclusive maximum length of an optional exemplar answer.</summary>
    public const int MaxSampleAnswerLength = 8000;

    private EssayItem(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        EssayRubric rubric,
        string? sampleAnswer)
        : base(ItemType.Essay, stem, difficulty, categoryId, maximumScore, authorId)
    {
        Rubric = rubric;
        SampleAnswer = sampleAnswer;
    }

    private EssayItem()
    {
    }

    /// <summary>Gets the grading guidance applied to a response.</summary>
    public EssayRubric Rubric { get; private set; } = null!;

    /// <summary>Gets the optional exemplar answer shown to graders.</summary>
    public string? SampleAnswer { get; private set; }

    /// <summary>Creates a draft essay item.</summary>
    /// <param name="stem">The prompt shown to the examinee.</param>
    /// <param name="difficulty">The cognitive demand of the item.</param>
    /// <param name="categoryId">The category the item is filed under.</param>
    /// <param name="maximumScore">The score a fully correct response is worth.</param>
    /// <param name="authorId">The author creating the item.</param>
    /// <param name="rubric">The grading guidance.</param>
    /// <param name="sampleAnswer">An optional exemplar answer.</param>
    /// <returns>The new draft item.</returns>
    /// <exception cref="DomainException">The exemplar answer was too long.</exception>
    public static EssayItem Create(
        ItemStem stem,
        DifficultyLevel difficulty,
        CategoryId categoryId,
        Points maximumScore,
        UserId authorId,
        EssayRubric rubric,
        string? sampleAnswer = null)
        => new(
            stem,
            difficulty,
            categoryId,
            maximumScore,
            authorId,
            rubric,
            NormalizeSampleAnswer(sampleAnswer));

    /// <summary>Replaces the grading guidance of a draft item.</summary>
    /// <param name="rubric">The new grading guidance.</param>
    /// <param name="sampleAnswer">The new exemplar answer, or <see langword="null"/> to clear it.</param>
    /// <exception cref="DomainException">The item is not editable or the exemplar was too long.</exception>
    public void UpdateRubric(EssayRubric rubric, string? sampleAnswer)
    {
        EnsureEditable();
        Rubric = rubric;
        SampleAnswer = NormalizeSampleAnswer(sampleAnswer);
    }

    /// <inheritdoc />
    protected override void EnsureContentIsComplete()
        => _ = Ensure.NotNull(Rubric, "item.rubric_required", "An essay item requires grading guidance.");

    /// <inheritdoc />
    protected override ItemVersionContent CaptureContent() => new([], Rubric);

    private static string? NormalizeSampleAnswer(string? sampleAnswer)
    {
        if (string.IsNullOrWhiteSpace(sampleAnswer))
        {
            return null;
        }

        var trimmed = sampleAnswer.Trim();
        Ensure.MaxLength(
            trimmed,
            MaxSampleAnswerLength,
            "item.sample_answer_too_long",
            $"A sample answer must not exceed {MaxSampleAnswerLength} characters.");
        return trimmed;
    }
}
