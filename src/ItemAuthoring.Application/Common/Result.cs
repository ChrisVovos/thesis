using System.Diagnostics.CodeAnalysis;

namespace ItemAuthoring.Application.Common;

/// <summary>
/// The outcome of a use case that either succeeded or failed for a known, expected reason.
/// </summary>
/// <remarks>
/// Expected failures — "not found", "already published", "not permitted" — are returned rather than
/// thrown. Exceptions remain reserved for genuinely exceptional conditions, which keeps the control
/// flow of a handler visible in its signature and removes the temptation to use exceptions to model
/// business outcomes.
/// </remarks>
public class Result
{
    /// <summary>Initializes a new instance of the <see cref="Result"/> class.</summary>
    /// <param name="isSuccess">Whether the use case succeeded.</param>
    /// <param name="error">The failure, or <see cref="Common.Error.None"/> on success.</param>
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the use case succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the use case failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure, or <see cref="Common.Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>Creates a successful result.</summary>
    /// <returns>The result.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>The result.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying a value.</summary>
    /// <typeparam name="TValue">The type of the carried value.</typeparam>
    /// <param name="value">The value produced by the use case.</param>
    /// <returns>The result.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>Creates a failed result of a value-carrying result type.</summary>
    /// <typeparam name="TValue">The type the result would have carried.</typeparam>
    /// <param name="error">The failure.</param>
    /// <returns>The result.</returns>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>
/// The outcome of a use case that produces a value when it succeeds.
/// </summary>
/// <typeparam name="TValue">The type of the produced value.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>Initializes a new instance of the <see cref="Result{TValue}"/> class.</summary>
    /// <param name="value">The produced value, or <see langword="null"/> on failure.</param>
    /// <param name="isSuccess">Whether the use case succeeded.</param>
    /// <param name="error">The failure, or <see cref="Common.Error.None"/> on success.</param>
    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>Gets the produced value.</summary>
    /// <exception cref="InvalidOperationException">The result represents a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be read.");

    /// <summary>Attempts to read the produced value.</summary>
    /// <param name="value">The produced value when the result succeeded.</param>
    /// <returns><see langword="true"/> when the result succeeded.</returns>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = IsSuccess ? _value : default;
        return IsSuccess && value is not null;
    }

    /// <summary>Lifts a value into a successful result.</summary>
    /// <param name="value">The value to lift.</param>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
