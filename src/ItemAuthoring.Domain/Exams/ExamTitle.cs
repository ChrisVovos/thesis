using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Exams;

/// <summary>
/// The title of an examination.
/// </summary>
public sealed record ExamTitle
{
    /// <summary>The inclusive maximum number of characters a title may contain.</summary>
    public const int MaxLength = 256;

    private ExamTitle(string value) => Value = value;

    /// <summary>Gets the title.</summary>
    public string Value { get; }

    /// <summary>Creates a validated title.</summary>
    /// <param name="value">The candidate title.</param>
    /// <returns>The validated title.</returns>
    /// <exception cref="DomainException">The title was blank or too long.</exception>
    public static ExamTitle Create(string? value)
    {
        var trimmed = Ensure.NotBlank(value, "exam.title_required", "An exam title is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "exam.title_too_long",
            $"An exam title must not exceed {MaxLength} characters.");
        return new ExamTitle(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
