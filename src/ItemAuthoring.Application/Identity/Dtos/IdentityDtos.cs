namespace ItemAuthoring.Application.Identity.Dtos;

/// <summary>A capability that can be granted to a role.</summary>
public sealed record PermissionDto
{
    /// <summary>Gets the identity of the permission.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the stable capability identifier.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the human readable explanation.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>A named bundle of permissions.</summary>
public sealed record RoleDto
{
    /// <summary>Gets the identity of the role.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the role name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the human readable explanation.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the role ships with the platform.</summary>
    public bool IsSystemRole { get; init; }

    /// <summary>Gets the permissions granted to the role.</summary>
    public IReadOnlyList<PermissionDto> Permissions { get; init; } = [];

    /// <summary>Gets the number of users currently holding the role.</summary>
    public int UserCount { get; init; }
}

/// <summary>A person who signs in to the platform.</summary>
public sealed record UserDto
{
    /// <summary>Gets the identity of the user.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the login identifier.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the human readable name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the user may sign in.</summary>
    public bool IsActive { get; init; }

    /// <summary>Gets the instant, in UTC, of the most recent successful sign-in.</summary>
    public DateTimeOffset? LastSignInAtUtc { get; init; }

    /// <summary>Gets the instant, in UTC, at which the account was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets the roles assigned to the user.</summary>
    public IReadOnlyList<RoleDto> Roles { get; init; } = [];
}

/// <summary>The identity of the caller together with the permissions they hold.</summary>
public sealed record CurrentUserDto
{
    /// <summary>Gets the identity of the caller.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the login identifier.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the human readable name.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Gets the role names held by the caller.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>Gets the permissions held by the caller.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}

/// <summary>The tokens issued after a successful sign-in or refresh.</summary>
public sealed record AuthenticationResultDto
{
    /// <summary>Gets the signed access token.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Gets the instant, in UTC, after which the access token is rejected.</summary>
    public DateTimeOffset AccessTokenExpiresAtUtc { get; init; }

    /// <summary>Gets the opaque refresh token.</summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Gets the instant, in UTC, after which the refresh token is rejected.</summary>
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; init; }

    /// <summary>Gets the profile of the authenticated user.</summary>
    public CurrentUserDto User { get; init; } = new();
}
