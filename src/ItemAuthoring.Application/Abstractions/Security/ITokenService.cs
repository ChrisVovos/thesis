using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Abstractions.Security;

/// <summary>
/// A freshly issued access token and its expiry.
/// </summary>
/// <param name="Value">The signed JSON Web Token.</param>
/// <param name="ExpiresAtUtc">The instant after which the token is rejected.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// A freshly issued refresh token, in both the form handed to the client and the form stored.
/// </summary>
/// <param name="Value">The opaque token handed to the client. It is never persisted.</param>
/// <param name="Hash">The Base64 encoded SHA-256 hash of <paramref name="Value"/>, which is stored.</param>
/// <param name="ExpiresAtUtc">The instant after which the token is rejected.</param>
public sealed record RefreshTokenMaterial(string Value, string Hash, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Issues the tokens that carry a user's identity and permissions between requests.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues an access token describing the supplied user.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="roles">The role names held by the user.</param>
    /// <param name="permissions">The permissions held by the user.</param>
    /// <returns>The signed access token.</returns>
    AccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);

    /// <summary>Generates a new refresh token.</summary>
    /// <returns>The token material.</returns>
    RefreshTokenMaterial CreateRefreshToken();

    /// <summary>Hashes a refresh token presented by a client so it can be compared with storage.</summary>
    /// <param name="token">The opaque token presented by the client.</param>
    /// <returns>The Base64 encoded SHA-256 hash.</returns>
    string HashRefreshToken(string token);
}
