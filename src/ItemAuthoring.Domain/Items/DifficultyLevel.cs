namespace ItemAuthoring.Domain.Items;

/// <summary>
/// The cognitive demand of an item, used when balancing an exam blueprint.
/// </summary>
public enum DifficultyLevel
{
    /// <summary>Recall of a single fact.</summary>
    VeryEasy = 1,

    /// <summary>Straightforward application of a single concept.</summary>
    Easy = 2,

    /// <summary>Application of a concept in a familiar context.</summary>
    Medium = 3,

    /// <summary>Analysis across several concepts.</summary>
    Hard = 4,

    /// <summary>Synthesis or evaluation in an unfamiliar context.</summary>
    VeryHard = 5,
}
