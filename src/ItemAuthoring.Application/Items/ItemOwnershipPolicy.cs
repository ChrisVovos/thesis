using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items;

/// <summary>
/// The ownership rule that supplements permission checks for item editing.
/// </summary>
/// <remarks>
/// Holding <see cref="Permissions.ItemsUpdate"/> answers "may this person edit items at all"; it
/// cannot answer "may this person edit <em>this</em> item". The second question depends on data and
/// therefore belongs in the application layer next to the loaded aggregate, not in a transport level
/// policy that has never seen the item.
/// </remarks>
public static class ItemOwnershipPolicy
{
    /// <summary>Determines whether the caller may edit or delete the supplied item.</summary>
    /// <param name="item">The item the caller wants to change.</param>
    /// <param name="currentUser">The principal on whose behalf the request executes.</param>
    /// <returns><see langword="true"/> when the caller owns the item or administers the platform.</returns>
    public static bool CanEdit(Item item, ICurrentUser currentUser)
        => currentUser.IsInRole(RoleNames.Administrator) || item.AuthorId == currentUser.UserId;
}
