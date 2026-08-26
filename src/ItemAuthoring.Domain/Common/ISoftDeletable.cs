namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Implemented by aggregates that are removed logically rather than physically, because assessment
/// content must remain auditable after it leaves circulation.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Gets a value indicating whether the aggregate has been logically removed.</summary>
    bool IsDeleted { get; }

    /// <summary>Gets the instant, in UTC, at which the aggregate was logically removed.</summary>
    DateTimeOffset? DeletedAtUtc { get; }
}
