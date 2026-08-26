using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <content>
/// Sign-in outcome tracking and refresh token lifecycle.
/// </content>
public sealed partial class User
{
    /// <summary>The number of consecutive failures that triggers a temporary lockout.</summary>
    public const int MaxFailedSignInAttempts = 5;

    /// <summary>The duration of a lockout, in minutes.</summary>
    public const int LockoutMinutes = 15;

    /// <summary>Determines whether sign-in is currently blocked by a lockout.</summary>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns><see langword="true"/> when the account is locked out.</returns>
    public bool IsLockedOut(DateTimeOffset nowUtc)
        => LockedOutUntilUtc is { } until && until > nowUtc;

    /// <summary>Records a successful sign-in and clears the failure counter.</summary>
    /// <param name="atUtc">The sign-in instant.</param>
    public void RecordSuccessfulSignIn(DateTimeOffset atUtc)
    {
        FailedSignInAttempts = 0;
        LockedOutUntilUtc = null;
        LastSignInAtUtc = atUtc;
    }

    /// <summary>Records a failed sign-in and locks the account once the threshold is reached.</summary>
    /// <param name="atUtc">The failure instant.</param>
    public void RecordFailedSignIn(DateTimeOffset atUtc)
    {
        FailedSignInAttempts++;
        if (FailedSignInAttempts >= MaxFailedSignInAttempts)
        {
            LockedOutUntilUtc = atUtc.AddMinutes(LockoutMinutes);
            FailedSignInAttempts = 0;
        }
    }

    /// <summary>Issues a refresh token to the user.</summary>
    /// <param name="tokenHash">The Base64 encoded SHA-256 hash of the token.</param>
    /// <param name="issuedAtUtc">The issue instant.</param>
    /// <param name="expiresAtUtc">The expiry instant.</param>
    /// <returns>The issued token.</returns>
    public RefreshToken IssueRefreshToken(
        string? tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var token = RefreshToken.Issue(Id, tokenHash, issuedAtUtc, expiresAtUtc);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>Exchanges an active refresh token for a new one.</summary>
    /// <param name="presentedHash">The hash of the token presented by the client.</param>
    /// <param name="replacementHash">The hash of the replacement token.</param>
    /// <param name="nowUtc">The current instant.</param>
    /// <param name="expiresAtUtc">The expiry instant of the replacement token.</param>
    /// <returns>The replacement token.</returns>
    /// <exception cref="DomainException">
    /// The presented token is unknown, already used or expired. Reuse of an already rotated token is
    /// treated as theft and revokes the entire token family.
    /// </exception>
    public RefreshToken RotateRefreshToken(
        string presentedHash,
        string replacementHash,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc)
    {
        var existing = _refreshTokens.Find(token =>
            string.Equals(token.TokenHash, presentedHash, StringComparison.Ordinal))
            ?? throw new DomainException(
                "auth.refresh_token_unknown",
                "The refresh token is not recognised.");

        if (!existing.IsActive(nowUtc))
        {
            RevokeAllRefreshTokens(nowUtc);
            throw new DomainException(
                "auth.refresh_token_reused",
                "The refresh token is no longer valid; all sessions have been revoked.");
        }

        existing.Revoke(nowUtc, replacementHash);
        return IssueRefreshToken(replacementHash, nowUtc, expiresAtUtc);
    }

    /// <summary>Revokes a single refresh token, for example at sign-out.</summary>
    /// <param name="tokenHash">The hash of the token to revoke.</param>
    /// <param name="atUtc">The revocation instant.</param>
    /// <returns><see langword="true"/> when a matching active token was revoked.</returns>
    public bool RevokeRefreshToken(string tokenHash, DateTimeOffset atUtc)
    {
        var existing = _refreshTokens.Find(token =>
            string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal));

        if (existing is null || !existing.IsActive(atUtc))
        {
            return false;
        }

        existing.Revoke(atUtc);
        return true;
    }

    /// <summary>Revokes every outstanding refresh token.</summary>
    /// <param name="atUtc">The revocation instant.</param>
    public void RevokeAllRefreshTokens(DateTimeOffset atUtc)
    {
        foreach (var token in _refreshTokens.Where(token => token.IsActive(atUtc)))
        {
            token.Revoke(atUtc);
        }
    }

    /// <summary>Discards refresh tokens that expired before the supplied instant.</summary>
    /// <param name="beforeUtc">The cut-off instant.</param>
    public void PruneRefreshTokens(DateTimeOffset beforeUtc)
        => _refreshTokens.RemoveAll(token => token.ExpiresAtUtc <= beforeUtc);
}
