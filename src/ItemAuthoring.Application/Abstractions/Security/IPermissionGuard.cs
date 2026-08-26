using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Abstractions.Security;

/// <summary>
/// Checks a permission outside the request pipeline.
/// </summary>
/// <remarks>
/// Most use cases declare their permission with <see cref="RequiresPermissionAttribute"/> and are
/// authorized by the pipeline. A GraphQL field that exposes a composable <see cref="IQueryable{T}"/>
/// cannot go through the pipeline — the whole point is that the caller's selection shapes the query —
/// so it asks this guard instead. The permission constant and the resulting error are the same in
/// both paths, which is what stops the two API surfaces from diverging.
/// </remarks>
public interface IPermissionGuard
{
    /// <summary>Verifies that the caller is authenticated and holds a permission.</summary>
    /// <param name="permission">The required permission, from <see cref="Permissions"/>.</param>
    /// <returns>Success when the caller holds the permission.</returns>
    Result Require(string permission);
}
