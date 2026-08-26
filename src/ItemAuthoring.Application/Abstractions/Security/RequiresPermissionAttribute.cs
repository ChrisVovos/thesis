namespace ItemAuthoring.Application.Abstractions.Security;

/// <summary>
/// Declares the permission a request requires.
/// </summary>
/// <remarks>
/// The attribute is read by the authorization pipeline behaviour, not by a controller filter or a
/// GraphQL directive, so a use case carries its own access rule wherever it is invoked from.
/// </remarks>
/// <param name="permission">The permission the caller must hold.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RequiresPermissionAttribute(string permission) : Attribute
{
    /// <summary>Gets the permission the caller must hold.</summary>
    public string Permission { get; } = permission;
}

/// <summary>
/// Marks a request as reachable without authentication.
/// </summary>
/// <remarks>
/// Requests are authenticated by default; opting out is explicit and auditable. Sign-in and token
/// refresh are the only use cases that carry this marker.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AllowAnonymousRequestAttribute : Attribute;
