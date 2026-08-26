using ItemAuthoring.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace ItemAuthoring.Api;

/// <summary>The CORS policies registered by the host.</summary>
public static class ApiCorsPolicies
{
    /// <summary>The policy that permits the Angular client to call both API surfaces.</summary>
    public const string Client = "item-authoring-client";
}

/// <summary>The hosting environments this application recognises beyond the standard three.</summary>
public static class ApiEnvironments
{
    /// <summary>
    /// The environment used for the comparative measurements.
    /// </summary>
    /// <remarks>
    /// It behaves like production — real SQL Server, real authentication, no developer conveniences —
    /// but keeps the transport selector and the benchmark endpoints enabled.
    /// </remarks>
    public const string Benchmark = "Benchmark";
}

/// <summary>Applies migrations and reference data at start-up.</summary>
public static class DatabaseSeedingExtensions
{
    /// <summary>Runs the database seeder in its own service scope.</summary>
    /// <param name="app">The built application.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (!options.Enabled && !options.ApplyMigrations)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
    }
}
