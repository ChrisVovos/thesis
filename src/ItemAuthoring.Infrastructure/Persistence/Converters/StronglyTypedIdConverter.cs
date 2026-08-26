using ItemAuthoring.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ItemAuthoring.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts a strongly typed identifier to and from the <see cref="Guid"/> stored in the database.
/// </summary>
/// <remarks>
/// One generic converter serves every identifier in the model, which is the reason
/// <see cref="IStronglyTypedId{TSelf}"/> declares a static factory. Without it, each aggregate would
/// need a hand written converter, and the cost of avoiding primitive obsession would grow with the
/// size of the model instead of staying constant.
/// </remarks>
/// <typeparam name="TId">The strongly typed identifier being converted.</typeparam>
public sealed class StronglyTypedIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId<TId>
{
    /// <summary>Initializes a new instance of the <see cref="StronglyTypedIdConverter{TId}"/> class.</summary>
    public StronglyTypedIdConverter()
        : base(id => id.Value, value => Wrap(value))
    {
    }

    // The static abstract factory cannot appear inside an expression tree, so it is reached through
    // an ordinary static method that the tree is allowed to call.
    private static TId Wrap(Guid value) => TId.From(value);
}
