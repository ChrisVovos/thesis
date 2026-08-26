namespace ItemAuthoring.Application.Common;

/// <summary>
/// The classification of a failure, independent of the transport that will report it.
/// </summary>
/// <remarks>
/// The REST surface maps these to HTTP status codes and the GraphQL surface maps them to error
/// extension classifications. Because both derive from the same value, a rule that is rejected as a
/// conflict over REST is also reported as a conflict over GraphQL — which is exactly what the
/// comparative study requires.
/// </remarks>
public enum ErrorType
{
    /// <summary>Input failed validation before any state was touched.</summary>
    Validation = 1,

    /// <summary>The addressed resource does not exist, or is not visible to the caller.</summary>
    NotFound = 2,

    /// <summary>The request conflicts with the current state of the resource.</summary>
    Conflict = 3,

    /// <summary>The caller did not present valid credentials.</summary>
    Unauthorized = 4,

    /// <summary>The caller is known but lacks the required permission.</summary>
    Forbidden = 5,

    /// <summary>An unexpected failure that the caller cannot correct.</summary>
    Failure = 6,
}
