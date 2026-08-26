namespace ItemAuthoring.Infrastructure.Identity;

/// <summary>
/// The claim types this application issues in addition to the registered JWT claims.
/// </summary>
public static class ApplicationClaimTypes
{
    /// <summary>A permission held by the caller. The claim repeats once per permission.</summary>
    public const string Permission = "permission";

    /// <summary>A role held by the caller. The claim repeats once per role.</summary>
    public const string Role = "role";
}
