namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The editorial lifecycle of an item.
/// </summary>
/// <remarks>
/// Legal transitions are enforced by <see cref="Item"/> itself; who may request a transition is
/// enforced by the application layer, so REST and GraphQL cannot diverge on either rule.
/// </remarks>
public enum ItemStatus
{
    /// <summary>The item is being authored and is freely editable.</summary>
    Draft = 1,

    /// <summary>The item has been submitted and is awaiting a reviewer decision.</summary>
    InReview = 2,

    /// <summary>A reviewer accepted the item; it may now be published.</summary>
    Approved = 3,

    /// <summary>The item is frozen, versioned and usable in exams.</summary>
    Published = 4,

    /// <summary>The item is withdrawn from further use but retained for audit.</summary>
    Retired = 5,
}
