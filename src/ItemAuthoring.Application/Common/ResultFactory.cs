using System.Collections.Concurrent;
using System.Reflection;

namespace ItemAuthoring.Application.Common;

/// <summary>
/// Builds a failed <see cref="Result"/> or <see cref="Result{TValue}"/> when only the closed
/// response type is known, as is the case inside a generic pipeline behaviour.
/// </summary>
/// <remarks>
/// Without this, a cross-cutting concern such as validation could only report a failure by throwing,
/// which would defeat the decision to model expected failures as return values. The reflection cost
/// is paid once per response type and then cached.
/// </remarks>
public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Factories = new();

    /// <summary>Determines whether a type is a result type this factory can produce.</summary>
    /// <param name="responseType">The candidate response type.</param>
    /// <returns><see langword="true"/> when a failure can be produced for the type.</returns>
    public static bool IsResultType(Type responseType)
        => responseType == typeof(Result)
            || (responseType.IsGenericType
                && responseType.GetGenericTypeDefinition() == typeof(Result<>));

    /// <summary>Creates a failed result of the requested response type.</summary>
    /// <typeparam name="TResponse">The response type of the pipeline.</typeparam>
    /// <param name="error">The failure to report.</param>
    /// <returns>The failed result.</returns>
    /// <exception cref="InvalidOperationException">The response type is not a result type.</exception>
    public static TResponse Failure<TResponse>(Error error)
    {
        var factory = Factories.GetOrAdd(typeof(TResponse), CreateFactory);
        return (TResponse)factory(error);
    }

    private static Func<Error, object> CreateFactory(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return static error => Result.Failure(error);
        }

        if (!IsResultType(responseType))
        {
            throw new InvalidOperationException(
                $"'{responseType}' is not a Result type; pipeline behaviours can only short-circuit "
                + "requests whose response is Result or Result<T>.");
        }

        var valueType = responseType.GetGenericArguments()[0];
        var method = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                candidate is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
            .MakeGenericMethod(valueType);

        return error => method.Invoke(null, [error])!;
    }
}
