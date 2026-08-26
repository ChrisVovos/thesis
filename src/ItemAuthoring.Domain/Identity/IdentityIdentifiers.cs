using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>Identifies a <see cref="User"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct UserId(Guid Value) : IStronglyTypedId<UserId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static UserId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static UserId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a <see cref="Role"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct RoleId(Guid Value) : IStronglyTypedId<RoleId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static RoleId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static RoleId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a <see cref="Permission"/>.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct PermissionId(Guid Value) : IStronglyTypedId<PermissionId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static PermissionId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static PermissionId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a <see cref="RefreshToken"/> issued to a user.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct RefreshTokenId(Guid Value) : IStronglyTypedId<RefreshTokenId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static RefreshTokenId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static RefreshTokenId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
