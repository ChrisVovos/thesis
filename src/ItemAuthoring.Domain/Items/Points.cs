using System.Globalization;
using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Items;

/// <summary>
/// A positive score contribution, expressed with at most two decimal places.
/// </summary>
public sealed record Points
{
    /// <summary>The largest score a single item may be worth.</summary>
    public const decimal MaxValue = 1000m;

    private Points(decimal value) => Value = value;

    /// <summary>Gets the numeric score.</summary>
    public decimal Value { get; }

    /// <summary>Creates a validated score.</summary>
    /// <param name="value">The candidate score.</param>
    /// <returns>The validated score.</returns>
    /// <exception cref="DomainException">The score was not a positive value within range.</exception>
    public static Points Create(decimal value)
    {
        Ensure.That(value > 0m, "item.points_not_positive", "Points must be greater than zero.");
        Ensure.That(
            value <= MaxValue,
            "item.points_out_of_range",
            $"Points must not exceed {MaxValue}.");
        Ensure.That(
            decimal.Round(value, 2) == value,
            "item.points_precision",
            "Points may have at most two decimal places.");
        return new Points(value);
    }

    /// <summary>Adds two scores.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The sum of both scores.</returns>
    public static Points operator +(Points left, Points right) => Create(left.Value + right.Value);

    /// <summary>Adds two scores.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The sum of both scores.</returns>
    public static Points Add(Points left, Points right) => left + right;

    /// <inheritdoc />
    public override string ToString() => Value.ToString("0.##", CultureInfo.InvariantCulture);
}
