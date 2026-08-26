using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The grading guidance attached to an essay item.
/// </summary>
public sealed record EssayRubric
{
    /// <summary>The inclusive maximum number of characters the guidance may contain.</summary>
    public const int MaxGuidanceLength = 4000;

    private EssayRubric(string guidance, int minimumWords, int maximumWords)
    {
        Guidance = guidance;
        MinimumWords = minimumWords;
        MaximumWords = maximumWords;
    }

    /// <summary>Gets the guidance a grader applies to a response.</summary>
    public string Guidance { get; }

    /// <summary>Gets the minimum number of words a response must contain.</summary>
    public int MinimumWords { get; }

    /// <summary>Gets the maximum number of words a response may contain.</summary>
    public int MaximumWords { get; }

    /// <summary>Creates a validated rubric.</summary>
    /// <param name="guidance">The grading guidance.</param>
    /// <param name="minimumWords">The minimum word count.</param>
    /// <param name="maximumWords">The maximum word count.</param>
    /// <returns>The validated rubric.</returns>
    /// <exception cref="DomainException">The guidance or the word bounds were invalid.</exception>
    public static EssayRubric Create(string? guidance, int minimumWords, int maximumWords)
    {
        var trimmed = Ensure.NotBlank(
            guidance,
            "item.rubric_required",
            "An essay item requires grading guidance.");
        Ensure.MaxLength(
            trimmed,
            MaxGuidanceLength,
            "item.rubric_too_long",
            $"Grading guidance must not exceed {MaxGuidanceLength} characters.");
        Ensure.That(
            minimumWords >= 0,
            "item.rubric_min_words_negative",
            "The minimum word count cannot be negative.");
        Ensure.That(
            maximumWords > minimumWords,
            "item.rubric_word_range_invalid",
            "The maximum word count must be greater than the minimum word count.");
        return new EssayRubric(trimmed, minimumWords, maximumWords);
    }
}
