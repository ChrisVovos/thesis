using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity;

/// <summary>
/// Assembles the response returned by sign-in and by token refresh.
/// </summary>
/// <remarks>
/// Both use cases return exactly the same shape, so the projection is written once. This is the sort
/// of mapping that does not justify a mapping framework: it is three lines, it is compile-time
/// checked, and a reader can see the whole contract without opening a profile class.
/// </remarks>
public static class AuthenticationResultFactory
{
    /// <summary>Builds the authentication response for a signed-in user.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="roles">The role names held by the user.</param>
    /// <param name="permissions">The permissions held by the user.</param>
    /// <param name="accessToken">The freshly issued access token.</param>
    /// <param name="refreshToken">The freshly issued refresh token.</param>
    /// <returns>The authentication response.</returns>
    public static AuthenticationResultDto Create(
        User user,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        AccessToken accessToken,
        RefreshTokenMaterial refreshToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(accessToken);
        ArgumentNullException.ThrowIfNull(refreshToken);

        return new AuthenticationResultDto
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Value,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = new CurrentUserDto
            {
                Id = user.Id.Value,
                Email = user.Email.Value,
                DisplayName = user.DisplayName.Value,
                Roles = roles,
                Permissions = permissions,
            },
        };
    }
}
