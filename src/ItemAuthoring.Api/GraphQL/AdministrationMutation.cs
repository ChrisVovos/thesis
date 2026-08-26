using HotChocolate;
using HotChocolate.Types;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Identity.Commands;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// The administration mutations, merged into the root mutation type.
/// </summary>
[ExtendObjectType<Mutation>]
public sealed class AdministrationMutation
{
    /// <summary>Creates a user account.</summary>
    /// <param name="input">The account to create.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new user.</returns>
    public async Task<Guid> CreateUser(
        CreateUserCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the profile and role assignment of a user.</summary>
    /// <param name="input">The new profile.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateUser(
        UpdateUserCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Activates or deactivates a user account.</summary>
    /// <param name="input">The requested activation state.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> SetUserActive(
        SetUserActiveCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces a user's password and revokes their outstanding sessions.</summary>
    /// <param name="input">The new password.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> ResetUserPassword(
        ResetUserPasswordCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Creates a role.</summary>
    /// <param name="input">The role to create.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new role.</returns>
    public async Task<Guid> CreateRole(
        CreateRoleCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Replaces the description and permission set of a role.</summary>
    /// <param name="input">The new definition.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> UpdateRole(
        UpdateRoleCommand input,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(input, cancellationToken)).UnwrapOrThrow();

    /// <summary>Deletes a role that no user holds.</summary>
    /// <param name="roleId">The identity of the role.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> on success.</returns>
    public async Task<bool> DeleteRole(
        Guid roleId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new DeleteRoleCommand(roleId), cancellationToken)).UnwrapOrThrow();
}
