using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Items.Events;

/// <summary>Raised when a new item has been authored.</summary>
/// <param name="ItemId">The identity of the new item.</param>
/// <param name="Type">The answer shape of the new item.</param>
/// <param name="AuthorId">The author who created the item.</param>
public sealed record ItemCreatedDomainEvent(ItemId ItemId, ItemType Type, UserId AuthorId)
    : DomainEvent;

/// <summary>Raised whenever an item moves between lifecycle states.</summary>
/// <param name="ItemId">The identity of the item.</param>
/// <param name="From">The previous status.</param>
/// <param name="To">The new status.</param>
public sealed record ItemStatusChangedDomainEvent(ItemId ItemId, ItemStatus From, ItemStatus To)
    : DomainEvent;

/// <summary>Raised when an item is published and an immutable version is captured.</summary>
/// <param name="ItemId">The identity of the item.</param>
/// <param name="VersionNumber">The version number that was frozen.</param>
public sealed record ItemPublishedDomainEvent(ItemId ItemId, int VersionNumber) : DomainEvent;

/// <summary>Raised when an item is logically removed from the bank.</summary>
/// <param name="ItemId">The identity of the item.</param>
public sealed record ItemDeletedDomainEvent(ItemId ItemId) : DomainEvent;
