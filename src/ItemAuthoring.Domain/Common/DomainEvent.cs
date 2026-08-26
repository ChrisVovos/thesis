namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Base class for domain events that supplies the timestamp required by <see cref="IDomainEvent"/>.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
