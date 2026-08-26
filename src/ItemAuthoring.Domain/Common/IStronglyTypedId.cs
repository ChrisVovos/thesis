namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Contract implemented by every strongly typed identifier in the model.
/// </summary>
/// <remarks>
/// The static abstract factory lets the persistence layer register a single generic value converter
/// for all identifiers instead of one hand written converter per type, which keeps the mapping code
/// proportional to the number of concepts rather than to the number of aggregates.
/// </remarks>
/// <typeparam name="TSelf">The implementing identifier type.</typeparam>
public interface IStronglyTypedId<TSelf>
    where TSelf : struct, IStronglyTypedId<TSelf>
{
    /// <summary>Gets the underlying database value.</summary>
    Guid Value { get; }

    /// <summary>Creates an identifier from its underlying database value.</summary>
    /// <param name="value">The underlying value.</param>
    /// <returns>The strongly typed identifier.</returns>
    static abstract TSelf From(Guid value);
}
