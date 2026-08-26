using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Abstractions.Security;

/// <summary>
/// Computes and verifies password hashes.
/// </summary>
/// <remarks>
/// The algorithm is an infrastructure decision. Keeping it behind this interface means the hash can
/// be strengthened later without any change to the identity aggregates or their use cases.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password.</summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The encoded hash.</returns>
    PasswordHash Hash(string password);

    /// <summary>Verifies a plaintext password against a stored hash.</summary>
    /// <param name="hash">The stored hash.</param>
    /// <param name="password">The plaintext password presented by the caller.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    bool Verify(PasswordHash hash, string password);
}
