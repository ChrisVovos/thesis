using System.Security.Cryptography;
using ItemAuthoring.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

namespace ItemAuthoring.Integration.Tests.Infrastructure;

/// <summary>
/// Starts one SQL Server container for the whole suite and hosts the real API pipeline against it.
/// </summary>
/// <remarks>
/// The application is started exactly as it is in production — same middleware, same authentication,
/// same GraphQL schema — so a test that passes here says something about the deployed system rather
/// than about a reconstruction of it.
/// </remarks>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
            .WithCleanUp(true)
            .Build();

    /// <summary>Gets the administrator password generated for this run.</summary>
    public string AdministratorPassword { get; } =
        "Aa1!" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(12));

    /// <summary>Gets the administrator login identifier used by the seeded account.</summary>
    public const string AdministratorEmail = "administrator@itemauthoring.test";

    /// <inheritdoc />
    async Task IAsyncLifetime.InitializeAsync()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        await _database.StartAsync();

        // Touching the server forces the host to build, migrate and seed before the first test runs.
        _ = Server;
    }

    /// <inheritdoc />
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (ContainerRuntime.IsAvailable)
        {
            await _database.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [$"ConnectionStrings:{InfrastructureServiceCollectionExtensions.ConnectionStringName}"] =
                    _database.GetConnectionString(),
                ["Jwt:Issuer"] = "https://itemauthoring.test",
                ["Jwt:Audience"] = "itemauthoring.test",
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["Seed:Enabled"] = "true",
                ["Seed:ApplyMigrations"] = "true",
                ["Seed:AdministratorEmail"] = AdministratorEmail,
                ["Seed:AdministratorPassword"] = AdministratorPassword,
                ["Seed:IncludeSampleContent"] = "true",
                ["Seed:SampleItemCount"] = "40",
                ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            }));
    }
}

/// <summary>Shares the container and the host across every integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    /// <summary>The name of the shared collection.</summary>
    public const string Name = "api";
}
