namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Base class for every entity in the model. Identity — not attribute equality — decides whether
/// two instances are the same thing.
/// </summary>
/// <typeparam name="TId">The strongly typed identifier of the entity.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEquatable<TId>
{
    /// <summary>Initializes a new entity with the supplied identity.</summary>
    /// <param name="id">The identity of the entity.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>Initializes a new entity for the persistence layer only.</summary>
    /// <remarks>Entity Framework Core materializes entities through this constructor.</remarks>
    protected Entity()
    {
    }

    /// <summary>Gets the identity of the entity.</summary>
    public TId Id { get; protected set; }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
        => other is not null && other.GetType() == GetType() && other.Id.Equals(Id);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>Determines whether two entities share the same identity.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both operands denote the same entity.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two entities denote different identities.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the operands denote different entities.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
