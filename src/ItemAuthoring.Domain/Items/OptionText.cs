using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The text of a single answer option.
/// </summary>
public sealed record OptionText
{
    /// <summary>The inclusive maximum number of characters an option may contain.</summary>
    public const int MaxLength = 1000;

    private OptionText(string text) => Text = text;

    /// <summary>Gets the option text.</summary>
    public string Text { get; }

    /// <summary>Creates a validated option text.</summary>
    /// <param name="text">The candidate option text.</param>
    /// <returns>The validated option text.</returns>
    /// <exception cref="DomainException">The text was blank or too long.</exception>
    public static OptionText Create(string? text)
    {
        var trimmed = Ensure.NotBlank(text, "item.option_text_required", "Option text is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "item.option_text_too_long",
            $"Option text must not exceed {MaxLength} characters.");
        return new OptionText(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Text;
}
