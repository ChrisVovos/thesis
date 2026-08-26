using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// A single capability that can be granted to a role.
/// </summary>
public sealed class Permission : AggregateRoot<PermissionId>
{
    /// <summary>The inclusive maximum length of a permission name.</summary>
    public const int MaxNameLength = 64;

    /// <summary>The inclusive maximum length of a permission description.</summary>
    public const int MaxDescriptionLength = 256;

    private Permission(PermissionId id, string name, string description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    private Permission()
    {
    }

    /// <summary>Gets the stable identifier of the capability, for example <c>items.publish</c>.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the human readable explanation shown in the administration screens.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Creates a permission.</summary>
    /// <param name="name">The stable capability identifier.</param>
    /// <param name="description">The human readable explanation.</param>
    /// <returns>The new permission.</returns>
    /// <exception cref="DomainException">The name or description were invalid.</exception>
    public static Permission Create(string? name, string? description)
    {
        var trimmedName = Ensure.NotBlank(
            name,
            "permission.name_required",
            "A permission name is required.");
        Ensure.MaxLength(
            trimmedName,
            MaxNameLength,
            "permission.name_too_long",
            $"A permission name must not exceed {MaxNameLength} characters.");
        var trimmedDescription = Ensure.NotBlank(
            description,
            "permission.description_required",
            "A permission description is required.");
        Ensure.MaxLength(
            trimmedDescription,
            MaxDescriptionLength,
            "permission.description_too_long",
            $"A permission description must not exceed {MaxDescriptionLength} characters.");
        return new Permission(PermissionId.New(), trimmedName, trimmedDescription);
    }
}
