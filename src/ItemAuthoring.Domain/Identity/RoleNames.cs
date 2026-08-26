namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The role names shipped with the platform.
/// </summary>
/// <remarks>
/// Roles are data — an administrator may create more — but these four are referenced by seeding and
/// by the default authorization policies, so they are named constants rather than magic strings.
/// </remarks>
public static class RoleNames
{
    /// <summary>Full control, including user and role administration.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Assembles and publishes examinations from approved items.</summary>
    public const string Instructor = "Instructor";

    /// <summary>Creates and maintains items.</summary>
    public const string Author = "Author";

    /// <summary>Approves or rejects items submitted for review.</summary>
    public const string Reviewer = "Reviewer";

    /// <summary>Gets every role name shipped with the platform.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Administrator, Instructor, Author, Reviewer];
}
