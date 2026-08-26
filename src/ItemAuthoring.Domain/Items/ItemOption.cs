using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A single selectable answer belonging to a choice item.
/// </summary>
/// <remarks>
/// Options are not aggregate roots: they have no life of their own and are only ever reached
/// through the owning <see cref="ChoiceItem"/>, which is what guarantees the "exactly one correct
/// answer" style invariants can never be bypassed.
/// </remarks>
public sealed class ItemOption : Entity<ItemOptionId>
{
    private ItemOption(ItemOptionId id, OptionText text, bool isCorrect, int position, string? feedback)
        : base(id)
    {
        Text = text;
        IsCorrect = isCorrect;
        Position = position;
        Feedback = feedback;
    }

    private ItemOption()
    {
    }

    /// <summary>The inclusive maximum number of characters of author feedback per option.</summary>
    public const int MaxFeedbackLength = 1000;

    /// <summary>Gets the identity of the item this option belongs to.</summary>
    public ItemId ItemId { get; private set; }

    /// <summary>Gets the option text shown to the examinee.</summary>
    public OptionText Text { get; private set; } = null!;

    /// <summary>Gets a value indicating whether selecting this option scores.</summary>
    public bool IsCorrect { get; private set; }

    /// <summary>Gets the zero based display position of the option.</summary>
    public int Position { get; private set; }

    /// <summary>Gets the optional rationale shown after the item is answered.</summary>
    public string? Feedback { get; private set; }

    /// <summary>Creates a validated option.</summary>
    /// <param name="text">The option text.</param>
    /// <param name="isCorrect">Whether selecting this option scores.</param>
    /// <param name="position">The zero based display position.</param>
    /// <param name="feedback">Optional rationale shown after answering.</param>
    /// <returns>The new option.</returns>
    /// <exception cref="DomainException">The position or feedback were invalid.</exception>
    public static ItemOption Create(string? text, bool isCorrect, int position, string? feedback = null)
    {
        Ensure.That(
            position >= 0,
            "item.option_position_negative",
            "An option position cannot be negative.");
        var normalizedFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        if (normalizedFeedback is not null)
        {
            Ensure.MaxLength(
                normalizedFeedback,
                MaxFeedbackLength,
                "item.option_feedback_too_long",
                $"Option feedback must not exceed {MaxFeedbackLength} characters.");
        }

        return new ItemOption(
            ItemOptionId.New(),
            OptionText.Create(text),
            isCorrect,
            position,
            normalizedFeedback);
    }

    /// <summary>Moves the option to a new display position.</summary>
    /// <param name="position">The new zero based display position.</param>
    internal void MoveTo(int position) => Position = position;

    /// <summary>Attaches the option to its owning item.</summary>
    /// <param name="itemId">The identity of the owning item.</param>
    internal void AttachTo(ItemId itemId) => ItemId = itemId;
}
