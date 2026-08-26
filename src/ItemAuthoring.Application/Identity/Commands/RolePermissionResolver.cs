using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity.Commands;

/// <summary>
/// Translates permission names supplied by a client into the identifiers the role aggregate expects.
/// </summary>
/// <remarks>
/// Clients name permissions (<c>items.publish</c>) because names are stable and readable; the
/// aggregate stores identifiers because that is what the relational grant table references. Doing the
/// translation in one place also gives a single, uniform "unknown permission" failure.
/// </remarks>
internal static class RolePermissionResolver
{
    /// <summary>Resolves permission names to identifiers.</summary>
    /// <param name="permissions">The permission catalogue.</param>
    /// <param name="names">The permission names supplied by the client.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The resolved identifiers, or a failure naming the unknown permissions.</returns>
    public static async Task<Result<IReadOnlyList<PermissionId>>> ResolveAsync(
        IPermissionRepository permissions,
        IReadOnlyList<string> names,
        CancellationToken cancellationToken)
    {
        var distinct = names
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinct.Count == 0)
        {
            return Result.Success<IReadOnlyList<PermissionId>>([]);
        }

        var resolved = await permissions.GetByNamesAsync(distinct, cancellationToken);
        if (resolved.Count != distinct.Count)
        {
            var known = resolved.Select(permission => permission.Name).ToHashSet(StringComparer.Ordinal);
            var unknown = distinct.Where(name => !known.Contains(name));
            return Result.Failure<IReadOnlyList<PermissionId>>(Error.Validation(
                "permission.unknown",
                $"Unknown permissions: {string.Join(", ", unknown)}."));
        }

        return Result.Success<IReadOnlyList<PermissionId>>(
            resolved.Select(permission => permission.Id).ToList());
    }
}
