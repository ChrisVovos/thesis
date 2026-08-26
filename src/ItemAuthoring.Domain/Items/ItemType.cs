namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The answer and scoring shapes supported by the authoring platform.
/// </summary>
/// <remarks>
/// The value is persisted as the table-per-hierarchy discriminator, so the numeric values are part
/// of the database contract and must never be reordered.
/// </remarks>
public enum ItemType
{
    /// <summary>A stem with several options of which exactly one is correct.</summary>
    MultipleChoiceSingleResponse = 1,

    /// <summary>A stem with several options of which one or more are correct.</summary>
    MultipleChoiceMultipleResponse = 2,

    /// <summary>A free text response graded against a rubric.</summary>
    Essay = 3,

    /// <summary>A binary decision such as true/false or agree/disagree.</summary>
    EitherOr = 4,
}
