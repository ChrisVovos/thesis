using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// Creates a context for the Entity Framework Core command line tools.
/// </summary>
/// <remarks>
/// Design-time tooling must be able to build the model without starting the web host, and therefore
/// without a real connection string or a signing key. The connection string below is only ever used
/// to choose the provider and to generate the migration SQL; it is never connected to. A real one can
/// still be supplied through <c>ITEMAUTHORING_MIGRATIONS_CONNECTION</c> when scripting a deployment.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>The environment variable that overrides the design-time connection string.</summary>
    public const string ConnectionStringVariable = "ITEMAUTHORING_MIGRATIONS_CONNECTION";

    /// <inheritdoc />
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ItemAuthoring;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer
                .MigrationsHistoryTable("__EFMigrationsHistory", "authoring"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
