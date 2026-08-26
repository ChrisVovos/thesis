using System.ComponentModel.DataAnnotations;

namespace ItemAuthoring.Infrastructure.Identity;

/// <summary>
/// The signing and lifetime settings of the token service.
/// </summary>
/// <remarks>
/// The signing key is never committed. It is supplied by user secrets during development and by the
/// platform secret store in every deployed environment, and the options are validated at startup so a
/// missing or weak key fails the process rather than silently producing forgeable tokens.
/// </remarks>
public sealed class JwtOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The inclusive minimum length of the signing key, in characters.</summary>
    public const int MinimumSigningKeyLength = 64;

    /// <summary>Gets or sets the issuer placed in, and required from, every token.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the audience placed in, and required from, every token.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the symmetric signing key.</summary>
    [Required]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the access token lifetime in minutes.</summary>
    [Range(1, 240)]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Gets or sets the refresh token lifetime in days.</summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
