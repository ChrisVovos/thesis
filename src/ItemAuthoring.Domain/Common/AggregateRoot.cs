using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Base class for aggregate roots: the only entities a repository may load or persist directly, and
/// the only entities allowed to raise domain events.
/// </summary>
/// <typeparam name="TId">The strongly typed identifier of the aggregate.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents, IAuditable
    where TId : struct, IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Initializes a new aggregate with the supplied identity.</summary>
    /// <param name="id">The identity of the aggregate.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Initializes a new aggregate for the persistence layer only.</summary>
    protected AggregateRoot()
    {
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <inheritdoc />
    public UserId? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? LastModifiedAtUtc { get; private set; }

    /// <inheritdoc />
    public UserId? LastModifiedBy { get; private set; }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc />
    public void MarkCreated(DateTimeOffset atUtc, UserId? by)
    {
        CreatedAtUtc = atUtc;
        CreatedBy ??= by;
    }

    /// <inheritdoc />
    public void MarkModified(DateTimeOffset atUtc, UserId? by)
    {
        LastModifiedAtUtc = atUtc;
        LastModifiedBy = by;
    }

    /// <summary>Records a domain event to be dispatched once the transaction commits.</summary>
    /// <param name="domainEvent">The event to record.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Records the user that created the aggregate, when it is known up front.</summary>
    /// <param name="by">The creating user.</param>
    protected void SetCreatedBy(UserId by) => CreatedBy = by;
}
