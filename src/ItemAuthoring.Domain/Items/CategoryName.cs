using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The display name of a category in the item bank taxonomy.
/// </summary>
public sealed record CategoryName
{
    /// <summary>The inclusive maximum number of characters a category name may contain.</summary>
    public const int MaxLength = 128;

    private CategoryName(string value) => Value = value;

    /// <summary>Gets the display name.</summary>
    public string Value { get; }

    /// <summary>Creates a validated category name.</summary>
    /// <param name="value">The candidate name.</param>
    /// <returns>The validated name.</returns>
    /// <exception cref="DomainException">The name was blank or too long.</exception>
    public static CategoryName Create(string? value)
    {
        var trimmed = Ensure.NotBlank(
            value,
            "category.name_required",
            "A category name is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "category.name_too_long",
            $"A category name must not exceed {MaxLength} characters.");
        return new CategoryName(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
