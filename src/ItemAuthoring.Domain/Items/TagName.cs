using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A free-form label attached to items. Tags carry both the label as typed by the author and a
/// normalized form, so that "Algebra" and "algebra" are recognised as the same tag.
/// </summary>
public sealed record TagName
{
    /// <summary>The inclusive maximum number of characters a tag may contain.</summary>
    public const int MaxLength = 64;

    private TagName(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    /// <summary>Gets the label as entered by the author.</summary>
    public string Value { get; }

    /// <summary>Gets the lower-cased form used for uniqueness and lookup.</summary>
    public string Normalized { get; }

    /// <summary>Creates a validated tag name.</summary>
    /// <param name="value">The candidate label.</param>
    /// <returns>The validated tag name.</returns>
    /// <exception cref="DomainException">The label was blank or too long.</exception>
    public static TagName Create(string? value)
    {
        var trimmed = Ensure.NotBlank(value, "tag.name_required", "A tag name is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "tag.name_too_long",
            $"A tag name must not exceed {MaxLength} characters.");
        return new TagName(trimmed, trimmed.ToLowerInvariant());
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
