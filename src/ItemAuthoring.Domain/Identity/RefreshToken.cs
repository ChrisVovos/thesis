using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// A single issued refresh token.
/// </summary>
/// <remarks>
/// Only a hash of the token is stored. A leaked database therefore does not yield usable tokens, and
/// rotation is recorded explicitly so that reuse of an already-rotated token can be detected and the
/// whole token family revoked.
/// </remarks>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    /// <summary>The exact number of characters of a Base64 encoded SHA-256 token hash.</summary>
    public const int HashLength = 44;

    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private RefreshToken()
    {
    }

    /// <summary>Gets the user the token was issued to.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets the Base64 encoded SHA-256 hash of the token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the instant, in UTC, at which the token was issued.</summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }

    /// <summary>Gets the instant, in UTC, after which the token is no longer accepted.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Gets the instant, in UTC, at which the token was revoked, when it was.</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Gets the hash of the token that replaced this one during rotation.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>Issues a token.</summary>
    /// <param name="userId">The user the token is issued to.</param>
    /// <param name="tokenHash">The Base64 encoded SHA-256 hash of the token.</param>
    /// <param name="issuedAtUtc">The issue instant.</param>
    /// <param name="expiresAtUtc">The expiry instant.</param>
    /// <returns>The issued token.</returns>
    /// <exception cref="DomainException">The hash was malformed or the lifetime was not positive.</exception>
    internal static RefreshToken Issue(
        UserId userId,
        string? tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var trimmed = Ensure.NotBlank(
            tokenHash,
            "auth.refresh_token_hash_required",
            "A refresh token hash is required.");
        Ensure.That(
            expiresAtUtc > issuedAtUtc,
            "auth.refresh_token_lifetime_invalid",
            "A refresh token must expire after it was issued.");
        return new RefreshToken(RefreshTokenId.New(), userId, trimmed, issuedAtUtc, expiresAtUtc);
    }

    /// <summary>Determines whether the token may still be exchanged.</summary>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns><see langword="true"/> when the token is neither revoked nor expired.</returns>
    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    /// <summary>Revokes the token.</summary>
    /// <param name="atUtc">The revocation instant.</param>
    /// <param name="replacedByTokenHash">The hash of the replacement token during rotation.</param>
    internal void Revoke(DateTimeOffset atUtc, string? replacedByTokenHash = null)
    {
        RevokedAtUtc ??= atUtc;
        ReplacedByTokenHash ??= replacedByTokenHash;
    }
}
