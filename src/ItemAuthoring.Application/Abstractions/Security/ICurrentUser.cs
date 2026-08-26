using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Abstractions.Security;

/// <summary>
/// The principal on whose behalf the current request executes.
/// </summary>
/// <remarks>
/// Both API surfaces populate this from the same validated JWT, and the application layer authorizes
/// against this abstraction alone. That is what makes it impossible for REST and GraphQL to enforce
/// different rules: neither surface is ever asked to make an authorization decision.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>Gets the identity of the caller, or <see langword="null"/> when anonymous.</summary>
    UserId? UserId { get; }

    /// <summary>Gets the login identifier of the caller, when authenticated.</summary>
    string? Email { get; }

    /// <summary>Gets a value indicating whether the caller presented valid credentials.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the roles held by the caller.</summary>
    IReadOnlySet<string> Roles { get; }

    /// <summary>Gets the permissions held by the caller.</summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>Determines whether the caller holds a permission.</summary>
    /// <param name="permission">The permission to test for.</param>
    /// <returns><see langword="true"/> when the caller holds the permission.</returns>
    bool HasPermission(string permission);

    /// <summary>Determines whether the caller holds a role.</summary>
    /// <param name="role">The role to test for.</param>
    /// <returns><see langword="true"/> when the caller holds the role.</returns>
    bool IsInRole(string role);
}
