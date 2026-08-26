using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Audit trail carried by every aggregate root.
/// </summary>
/// <remarks>
/// The values are written by a persistence interceptor rather than by the aggregates themselves.
/// Keeping the clock and the current principal out of the domain means aggregate behaviour stays
/// deterministic and unit testable without a time abstraction threaded through every method.
/// </remarks>
public interface IAuditable
{
    /// <summary>Gets the instant, in UTC, at which the aggregate was first persisted.</summary>
    DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the user who created the aggregate, when known.</summary>
    UserId? CreatedBy { get; }

    /// <summary>Gets the instant, in UTC, of the most recent change.</summary>
    DateTimeOffset? LastModifiedAtUtc { get; }

    /// <summary>Gets the user who last changed the aggregate, when known.</summary>
    UserId? LastModifiedBy { get; }

    /// <summary>Stamps the aggregate as created.</summary>
    /// <param name="atUtc">The creation instant.</param>
    /// <param name="by">The acting user, when known.</param>
    void MarkCreated(DateTimeOffset atUtc, UserId? by);

    /// <summary>Stamps the aggregate as modified.</summary>
    /// <param name="atUtc">The modification instant.</param>
    /// <param name="by">The acting user, when known.</param>
    void MarkModified(DateTimeOffset atUtc, UserId? by);
}
