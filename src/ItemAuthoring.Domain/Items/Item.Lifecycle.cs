using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items.Events;

namespace ItemAuthoring.Domain.Items;

/// <content>
/// The editorial lifecycle of an item, expressed as an explicit state machine.
/// </content>
/// <remarks>
/// Keeping the legal transitions in one table rather than scattering <c>if (Status == ...)</c>
/// checks across the transition methods means the rule set can be read, tested and extended in a
/// single place, and it is the same rule set no matter which API surface requested the change.
/// </remarks>
public abstract partial class Item
{
    private static readonly Dictionary<ItemStatus, ItemStatus[]> AllowedTransitions = new()
    {
        [ItemStatus.Draft] = [ItemStatus.InReview],
        [ItemStatus.InReview] = [ItemStatus.Approved, ItemStatus.Draft],
        [ItemStatus.Approved] = [ItemStatus.Published, ItemStatus.Draft],
        [ItemStatus.Published] = [ItemStatus.Draft, ItemStatus.Retired],
        [ItemStatus.Retired] = [ItemStatus.Draft],
    };

    /// <summary>Submits a complete draft for reviewer attention.</summary>
    /// <exception cref="DomainException">The item is incomplete or not in draft.</exception>
    public void SubmitForReview()
    {
        EnsureNotDeleted();
        EnsureContentIsComplete();
        TransitionTo(ItemStatus.InReview);
    }

    /// <summary>Records a reviewer's acceptance of the item.</summary>
    /// <exception cref="DomainException">The item is not under review.</exception>
    public void Approve()
    {
        EnsureNotDeleted();
        TransitionTo(ItemStatus.Approved);
    }

    /// <summary>Returns the item to its author for further work.</summary>
    /// <exception cref="DomainException">The current status does not allow the transition.</exception>
    public void ReturnToDraft()
    {
        EnsureNotDeleted();
        TransitionTo(ItemStatus.Draft);
    }

    /// <summary>Freezes an approved item as a new immutable version.</summary>
    /// <param name="publishedAtUtc">The publication instant.</param>
    /// <exception cref="DomainException">The item has not been approved.</exception>
    public void Publish(DateTimeOffset publishedAtUtc)
    {
        EnsureNotDeleted();
        EnsureContentIsComplete();
        TransitionTo(ItemStatus.Published);

        VersionNumber++;
        _versions.Add(ItemVersion.Capture(this, VersionNumber, CaptureContent(), publishedAtUtc));
        Raise(new ItemPublishedDomainEvent(Id, VersionNumber));
    }

    /// <summary>Withdraws a published item from further use while retaining its history.</summary>
    /// <exception cref="DomainException">The item is not published.</exception>
    public void Retire()
    {
        EnsureNotDeleted();
        TransitionTo(ItemStatus.Retired);
    }

    /// <summary>Logically removes the item from the bank.</summary>
    /// <param name="deletedAtUtc">The deletion instant.</param>
    /// <exception cref="DomainException">The item is published and must be retired first.</exception>
    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        Ensure.That(
            Status is not ItemStatus.Published,
            "item.delete_published",
            "A published item must be retired before it can be deleted.");

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        Raise(new ItemDeletedDomainEvent(Id));
    }

    /// <summary>Reverses a logical deletion.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }

    private void EnsureNotDeleted()
        => Ensure.That(!IsDeleted, "item.deleted", "A deleted item cannot be modified.");

    private void TransitionTo(ItemStatus target)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = AllowedTransitions.TryGetValue(Status, out var targets) && targets.Contains(target);
        Ensure.That(
            allowed,
            "item.invalid_transition",
            $"An item cannot move from '{Status}' to '{target}'.");

        var previous = Status;
        Status = target;
        Raise(new ItemStatusChangedDomainEvent(Id, previous, target));
    }
}
