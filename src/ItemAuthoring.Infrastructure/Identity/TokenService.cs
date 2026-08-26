using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ItemAuthoring.Infrastructure.Identity;

/// <summary>
/// Issues signed access tokens and opaque refresh tokens.
/// </summary>
/// <remarks>
/// Roles and permissions are placed in the access token so that authorization needs no database round
/// trip per request. The consequence — a revoked permission stays effective until the short access
/// token expires — is accepted deliberately and bounded by
/// <see cref="JwtOptions.AccessTokenMinutes"/>; security-critical revocation (deactivating a user,
/// changing a password) additionally revokes every refresh token, so the session cannot be renewed.
/// </remarks>
/// <param name="options">The signing and lifetime settings.</param>
/// <param name="clock">The clock supplying issue and expiry instants.</param>
internal sealed class TokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    /// <inheritdoc />
    public AccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(permissions);

        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.DisplayName.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        claims.AddRange(roles.Select(role => new Claim(ApplicationClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(
            permission => new Claim(ApplicationClaimTypes.Permission, permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = credentials,
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    /// <inheritdoc />
    public RefreshTokenMaterial CreateRefreshToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenMaterial(
            value,
            HashRefreshToken(value),
            clock.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    /// <inheritdoc />
    public string HashRefreshToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
