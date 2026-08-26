using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity.Commands;

/// <summary>Creates a role and grants it a set of permissions.</summary>
/// <param name="Name">The role name.</param>
/// <param name="Description">The human readable explanation.</param>
/// <param name="PermissionNames">The permissions to grant.</param>
[RequiresPermission(Permissions.RolesManage)]
public sealed record CreateRoleCommand(
    string Name,
    string Description,
    IReadOnlyList<string> PermissionNames) : ICommand<Result<Guid>>;

/// <summary>Replaces the description and permission set of a role.</summary>
/// <param name="RoleId">The role to update.</param>
/// <param name="Name">The new role name; ignored for system roles.</param>
/// <param name="Description">The new description.</param>
/// <param name="PermissionNames">The permissions the role should hold afterwards.</param>
[RequiresPermission(Permissions.RolesManage)]
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string Description,
    IReadOnlyList<string> PermissionNames) : ICommand<Result>;

/// <summary>Deletes a role that no user holds.</summary>
/// <param name="RoleId">The role to delete.</param>
[RequiresPermission(Permissions.RolesManage)]
public sealed record DeleteRoleCommand(Guid RoleId) : ICommand<Result>;

/// <summary>Validates <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateRoleCommandValidator"/> class.</summary>
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Role.MaxNameLength);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(Role.MaxDescriptionLength);
        RuleFor(command => command.PermissionNames).NotNull();
    }
}

/// <summary>Validates <see cref="UpdateRoleCommand"/>.</summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateRoleCommandValidator"/> class.</summary>
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Role.MaxNameLength);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(Role.MaxDescriptionLength);
        RuleFor(command => command.PermissionNames).NotNull();
    }
}

/// <summary>Handles <see cref="CreateRoleCommand"/>.</summary>
/// <param name="roles">The role repository.</param>
/// <param name="permissions">The permission catalogue.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (await roles.NameExistsAsync(request.Name.Trim(), null, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "role.name_taken",
                "A role with that name already exists."));
        }

        var resolved = await RolePermissionResolver.ResolveAsync(
            permissions, request.PermissionNames, cancellationToken);
        if (resolved.IsFailure)
        {
            return Result.Failure<Guid>(resolved.Error);
        }

        var role = Role.Create(request.Name, request.Description);
        role.ReplacePermissions(resolved.Value);
        roles.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(role.Id.Value);
    }
}

/// <summary>Handles <see cref="UpdateRoleCommand"/>.</summary>
/// <param name="roles">The role repository.</param>
/// <param name="permissions">The permission catalogue.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class UpdateRoleCommandHandler(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRoleCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.RoleId);
        var role = await roles.GetAsync(roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound("role.not_found", "The role does not exist."));
        }

        if (!role.IsSystemRole)
        {
            if (await roles.NameExistsAsync(request.Name.Trim(), roleId, cancellationToken))
            {
                return Result.Failure(Error.Conflict(
                    "role.name_taken",
                    "A role with that name already exists."));
            }

            role.Rename(request.Name);
        }

        var resolved = await RolePermissionResolver.ResolveAsync(
            permissions, request.PermissionNames, cancellationToken);
        if (resolved.IsFailure)
        {
            return resolved;
        }

        role.Describe(request.Description);
        role.ReplacePermissions(resolved.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="DeleteRoleCommand"/>.</summary>
/// <param name="roles">The role repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class DeleteRoleCommandHandler(IRoleRepository roles, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var roleId = new RoleId(request.RoleId);
        var role = await roles.GetAsync(roleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound("role.not_found", "The role does not exist."));
        }

        if (role.IsSystemRole)
        {
            return Result.Failure(Error.Conflict(
                "role.system_immutable",
                "A role that ships with the platform cannot be deleted."));
        }

        if (await roles.IsAssignedAsync(roleId, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "role.in_use",
                "The role is still assigned to at least one user."));
        }

        roles.Remove(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
