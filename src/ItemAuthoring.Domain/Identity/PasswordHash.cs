using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The opaque result of running a password through a key derivation function.
/// </summary>
/// <remarks>
/// The domain deliberately knows nothing about the algorithm used. Hashing is a concern of the
/// infrastructure layer; the domain only guarantees that a plaintext password is never stored on a
/// <see cref="User"/> because the type system offers nowhere to put one.
/// </remarks>
public sealed record PasswordHash
{
    /// <summary>The inclusive maximum number of characters a stored hash may occupy.</summary>
    public const int MaxLength = 512;

    private PasswordHash(string value) => Value = value;

    /// <summary>Gets the encoded hash.</summary>
    public string Value { get; }

    /// <summary>Wraps an already computed hash.</summary>
    /// <param name="value">The encoded hash produced by the infrastructure layer.</param>
    /// <returns>The wrapped hash.</returns>
    /// <exception cref="DomainException">The hash was blank or implausibly long.</exception>
    public static PasswordHash FromHash(string? value)
    {
        var trimmed = Ensure.NotBlank(
            value,
            "user.password_hash_required",
            "A password hash is required.");
        Ensure.MaxLength(
            trimmed,
            MaxLength,
            "user.password_hash_too_long",
            $"A password hash must not exceed {MaxLength} characters.");
        return new PasswordHash(trimmed);
    }

    /// <inheritdoc />
    public override string ToString() => "***";
}
