using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ItemAuthoring.Api.Rest;

/// <summary>
/// The rate limiting policies applied to the API surfaces.
/// </summary>
/// <remarks>
/// Sign-in and token refresh are limited far more aggressively than ordinary traffic, because they
/// are the endpoints an attacker uses to guess credentials. The limit is keyed on the client address
/// so that one abusive client cannot lock out the rest of the tenant.
/// </remarks>
public static class RateLimitingPolicies
{
    /// <summary>The policy protecting the authentication endpoints.</summary>
    public const string Authentication = "authentication";

    /// <summary>The policy applied to every other endpoint.</summary>
    public const string Standard = "standard";

    /// <summary>Registers the rate limiting policies.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Authentication, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            options.AddPolicy(Standard, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 600,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });
    }

    private static string PartitionKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
