using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Tests.TestDoubles;

/// <summary>A clock that never moves unless a test moves it.</summary>
/// <param name="utcNow">The instant the clock reports.</param>
internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public static FixedClock At(int year, int month, int day)
        => new(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero));

    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

/// <summary>A principal assembled by a test rather than by a token.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public static FakeCurrentUser Anonymous() => new();

    public static FakeCurrentUser With(params string[] permissions) => new()
    {
        UserId = Domain.Identity.UserId.New(),
        Email = "tester@itemauthoring.local",
        IsAuthenticated = true,
        Permissions = permissions.ToHashSet(StringComparer.Ordinal),
    };

    public static FakeCurrentUser Administrator() => new()
    {
        UserId = Domain.Identity.UserId.New(),
        Email = "administrator@itemauthoring.local",
        IsAuthenticated = true,
        Roles = new HashSet<string>(StringComparer.Ordinal) { RoleNames.Administrator },
        Permissions = Domain.Identity.Permissions.All.ToHashSet(StringComparer.Ordinal),
    };

    public UserId? UserId { get; set; }

    public string? Email { get; set; }

    public bool IsAuthenticated { get; set; }

    public IReadOnlySet<string> Roles { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlySet<string> Permissions { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public bool IsInRole(string role) => Roles.Contains(role);
}
