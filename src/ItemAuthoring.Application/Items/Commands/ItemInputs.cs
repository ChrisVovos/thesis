namespace ItemAuthoring.Application.Items.Commands;

/// <summary>An answer option supplied by a client when authoring a choice item.</summary>
/// <param name="Text">The option text.</param>
/// <param name="IsCorrect">Whether selecting the option scores.</param>
/// <param name="Feedback">An optional rationale shown after answering.</param>
public sealed record ItemOptionInput(string Text, bool IsCorrect, string? Feedback = null);

/// <summary>Grading guidance supplied by a client when authoring an essay item.</summary>
/// <param name="Guidance">The guidance a grader applies to a response.</param>
/// <param name="MinimumWords">The minimum number of words a response must contain.</param>
/// <param name="MaximumWords">The maximum number of words a response may contain.</param>
public sealed record EssayRubricInput(string Guidance, int MinimumWords, int MaximumWords);
