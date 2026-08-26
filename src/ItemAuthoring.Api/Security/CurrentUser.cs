using System.Security.Claims;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Infrastructure.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ItemAuthoring.Api.Security;

/// <summary>
/// Exposes the authenticated principal of the current request to the application layer.
/// </summary>
/// <remarks>
/// Both API surfaces share this one implementation. REST populates <c>HttpContext.User</c> through
/// the JWT bearer handler, and so does GraphQL, because Hot Chocolate runs on the same ASP.NET Core
/// pipeline. Neither surface can therefore present a different principal for the same token.
/// </remarks>
/// <param name="httpContextAccessor">The accessor for the current request context.</param>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal = httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public UserId? UserId
        => Guid.TryParse(_principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var value)
            ? new UserId(value)
            : null;

    /// <inheritdoc />
    public string? Email => _principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

    /// <inheritdoc />
    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated is true && UserId is not null;

    /// <inheritdoc />
    public IReadOnlySet<string> Roles => Collect(ApplicationClaimTypes.Role);

    /// <inheritdoc />
    public IReadOnlySet<string> Permissions => Collect(ApplicationClaimTypes.Permission);

    /// <inheritdoc />
    public bool HasPermission(string permission) => Permissions.Contains(permission);

    /// <inheritdoc />
    public bool IsInRole(string role) => Roles.Contains(role);

    private HashSet<string> Collect(string claimType)
        => _principal is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : _principal.FindAll(claimType)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.Ordinal);
}
