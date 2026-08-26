using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The prompt an examinee reads. Modelled as a value object so that "a question has text" cannot
/// degrade into "a question has a string that might be empty".
/// </summary>
public sealed record ItemStem
{
    /// <summary>The inclusive maximum number of characters a stem may contain.</summary>
    public const int MaxLength = 4000;

    private ItemStem(string text) => Text = text;

    /// <summary>Gets the stem text.</summary>
    public string Text { get; }

    /// <summary>Creates a validated stem.</summary>
    /// <param name="text">The candidate stem text.</param>
    /// <returns>The validated stem.</returns>
    /// <exception cref="DomainException">The text was blank or too long.</exception>
    public static ItemStem Create(string? text)
    {
        var trimmed = Ensure.NotBlank(text, "item.stem_required", "The item stem is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "item.stem_too_long",
            $"The item stem must not exceed {MaxLength} characters.");
        return new ItemStem(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Text;
}
