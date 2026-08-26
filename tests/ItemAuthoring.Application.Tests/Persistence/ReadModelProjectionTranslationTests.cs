using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Persistence;

/// <summary>
/// Guards the projection strategy used by every read store.
/// </summary>
/// <remarks>
/// The read side projects strongly typed identifiers down to <see cref="Guid"/> so that DTOs never
/// leak domain types to the API surfaces. That only works if Entity Framework Core can translate the
/// unwrapping into SQL; if it silently fell back to client evaluation, GraphQL filtering would stop
/// reaching the database and every payload measurement in the study would be meaningless.
/// <c>ToQueryString</c> compiles the query without opening a connection, so this runs anywhere.
/// </remarks>
public sealed class ReadModelProjectionTranslationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\translation-probe;Database=probe")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Unwrapping_a_strongly_typed_identifier_translates_to_sql()
    {
        using var context = CreateContext();

        var sql = context.Items
            .Select(item => new { Id = item.Id.Value, Stem = item.Stem.Text })
            .ToQueryString();

        sql.ShouldContain("SELECT");
    }

    [Fact]
    public void Filtering_after_a_projection_translates_to_sql()
    {
        using var context = CreateContext();

        var sql = context.Items
            .Select(item => new { Id = item.Id.Value, item.Status })
            .Where(projection => projection.Status == ItemStatus.Published)
            .ToQueryString();

        sql.ShouldContain("WHERE");
    }
}
