namespace ItemAuthoring.Application.Common;

/// <summary>
/// A transport-neutral description of why a use case did not succeed.
/// </summary>
/// <param name="Code">The stable, machine readable identifier of the failure.</param>
/// <param name="Message">The human readable explanation.</param>
/// <param name="Type">The classification used to choose a transport specific status.</param>
/// <param name="Details">Per-field messages, populated for validation failures.</param>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    /// <summary>The error used when a result is successful; it is never surfaced to a caller.</summary>
    public static Error None { get; } = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Creates a validation failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <param name="details">Per-field messages.</param>
    /// <returns>The error.</returns>
    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? details = null)
        => new(code, message, ErrorType.Validation, details);

    /// <summary>Creates a "resource does not exist" failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a "conflicts with current state" failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    /// <summary>Creates a "credentials missing or invalid" failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a "permission denied" failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates an unexpected failure.</summary>
    /// <param name="code">The stable failure identifier.</param>
    /// <param name="message">The human readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error Failure(string code, string message)
        => new(code, message, ErrorType.Failure);
}
