using System.Text.RegularExpressions;
using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// A syntactically valid e-mail address, which is also the login identifier of a user.
/// </summary>
public sealed partial record EmailAddress
{
    /// <summary>The inclusive maximum number of characters an address may contain.</summary>
    public const int MaxLength = 254;

    private EmailAddress(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    /// <summary>Gets the address as entered.</summary>
    public string Value { get; }

    /// <summary>Gets the upper-cased form used for uniqueness and lookup.</summary>
    public string Normalized { get; }

    /// <summary>Creates a validated address.</summary>
    /// <param name="value">The candidate address.</param>
    /// <returns>The validated address.</returns>
    /// <exception cref="DomainException">The address was blank, too long or malformed.</exception>
    public static EmailAddress Create(string? value)
    {
        var trimmed = Ensure.NotBlank(value, "user.email_required", "An e-mail address is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "user.email_too_long",
            $"An e-mail address must not exceed {MaxLength} characters.");
        Ensure.That(
            Pattern().IsMatch(trimmed),
            "user.email_invalid",
            "The e-mail address is not a valid address.");
        return new EmailAddress(trimmed, trimmed.ToUpperInvariant());
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.None, matchTimeoutMilliseconds: 200)]
    private static partial Regex Pattern();
}
