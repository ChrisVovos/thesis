namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Marker contract for a fact that has already happened inside the domain model.
/// </summary>
/// <remarks>
/// Domain events are raised by aggregates while a use case executes and are dispatched only after
/// the surrounding transaction has been committed, so a handler can never observe a state that was
/// subsequently rolled back.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>Gets the instant, in UTC, at which the event was raised.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
