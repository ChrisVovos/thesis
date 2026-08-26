using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Exams;

/// <summary>Identifies an <see cref="Exam"/> aggregate.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ExamId(Guid Value) : IStronglyTypedId<ExamId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ExamId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ExamId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies an <see cref="ExamSection"/> inside an exam.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ExamSectionId(Guid Value) : IStronglyTypedId<ExamSectionId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ExamSectionId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ExamSectionId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies an <see cref="ExamItem"/>, the placement of an item inside a section.</summary>
/// <param name="Value">The underlying database value.</param>
public readonly record struct ExamItemId(Guid Value) : IStronglyTypedId<ExamItemId>
{
    /// <summary>Creates a new, time ordered identifier.</summary>
    /// <returns>The new identifier.</returns>
    public static ExamItemId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public static ExamItemId From(Guid value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
