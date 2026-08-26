using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The human readable name of a user, shown throughout the authoring interface.
/// </summary>
public sealed record DisplayName
{
    /// <summary>The inclusive maximum number of characters a display name may contain.</summary>
    public const int MaxLength = 128;

    private DisplayName(string value) => Value = value;

    /// <summary>Gets the display name.</summary>
    public string Value { get; }

    /// <summary>Creates a validated display name.</summary>
    /// <param name="value">The candidate display name.</param>
    /// <returns>The validated display name.</returns>
    /// <exception cref="DomainException">The name was blank or too long.</exception>
    public static DisplayName Create(string? value)
    {
        var trimmed = Ensure.NotBlank(
            value,
            "user.display_name_required",
            "A display name is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "user.display_name_too_long",
            $"A display name must not exceed {MaxLength} characters.");
        return new DisplayName(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
