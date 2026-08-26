using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace ItemAuthoring.Infrastructure.Identity;

/// <summary>
/// Hashes passwords with the ASP.NET Core PBKDF2 implementation.
/// </summary>
/// <remarks>
/// <para>
/// The platform hasher is used rather than a hand rolled one: it applies PBKDF2-HMAC-SHA512 with a
/// per-password salt and a versioned format byte, and it compares in constant time. Rolling a custom
/// scheme here would add risk without adding value.
/// </para>
/// <para>
/// <see cref="Verify"/> deliberately reports success for a rehash-needed result as well. The format
/// byte lets the iteration count be raised later without invalidating existing passwords.
/// </para>
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <inheritdoc />
    public PasswordHash Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return PasswordHash.FromHash(_hasher.HashPassword(default!, password));
    }

    /// <inheritdoc />
    public bool Verify(PasswordHash hash, string password)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var outcome = _hasher.VerifyHashedPassword(default!, hash.Value, password);
        return outcome is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
