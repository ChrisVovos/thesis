using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>Identifies an <see cref="Item"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ItemId(Guid Value) : IStronglyTypedId<ItemId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ItemId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ItemId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies an <see cref="ItemOption"/> belonging to a choice item.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ItemOptionId(Guid Value) : IStronglyTypedId<ItemOptionId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ItemOptionId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ItemOptionId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies an immutable <see cref="ItemVersion"/> snapshot.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ItemVersionId(Guid Value) : IStronglyTypedId<ItemVersionId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ItemVersionId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ItemVersionId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a <see cref="Category"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct CategoryId(Guid Value) : IStronglyTypedId<CategoryId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static CategoryId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static CategoryId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a <see cref="Tag"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct TagId(Guid Value) : IStronglyTypedId<TagId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static TagId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static TagId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
