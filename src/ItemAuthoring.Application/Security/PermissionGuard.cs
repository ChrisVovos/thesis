using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;

namespace ItemAuthoring.Application.Security;

/// <summary>
/// The single implementation of <see cref="IPermissionGuard"/>.
/// </summary>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class PermissionGuard(ICurrentUser currentUser) : IPermissionGuard
{
    /// <inheritdoc />
    public Result Require(string permission)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure(Error.Unauthorized(
                "auth.required",
                "Authentication is required to perform this operation."));
        }

        return currentUser.HasPermission(permission)
            ? Result.Success()
            : Result.Failure(Error.Forbidden(
                "auth.forbidden",
                $"The operation requires the '{permission}' permission."));
    }
}
