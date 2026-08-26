using System.Net;
using System.Net.Http.Json;
using ItemAuthoring.Integration.Tests.Infrastructure;
using Shouldly;

namespace ItemAuthoring.Integration.Tests;

/// <summary>
/// Exercises the authentication and authorization paths end to end against a real database.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthenticationTests(ApiFixture fixture)
{
    [RequiresDockerFact]
    public async Task Valid_credentials_return_a_usable_token_pair()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var profile = await client.Http.GetFromJsonAsync<CurrentUserResponse>(
            "/api/v1/auth/me",
            ApiClient.Json);

        profile!.Email.ShouldBe(ApiFixture.AdministratorEmail);
        profile.Roles.ShouldContain("Administrator");
        profile.Permissions.ShouldContain("items.create");
    }

    [RequiresDockerFact]
    public async Task Invalid_credentials_are_rejected_without_revealing_which_part_was_wrong()
    {
        var client = fixture.CreateClient();

        var unknownAccount = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "nobody@itemauthoring.test", password = "Wrong-Password-1!" },
            ApiClient.Json);

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = ApiFixture.AdministratorEmail, password = "Wrong-Password-1!" },
            ApiClient.Json);

        unknownAccount.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var first = await unknownAccount.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);
        var second = await wrongPassword.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);
        first!.Detail.ShouldBe(second!.Detail);
    }

    [RequiresDockerFact]
    public async Task A_refresh_token_can_be_exchanged_once_and_only_once()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var first = await client.Http.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = client.RefreshToken },
            ApiClient.Json);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var replay = await client.Http.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = client.RefreshToken },
            ApiClient.Json);

        replay.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [RequiresDockerFact]
    public async Task Signing_out_invalidates_the_refresh_token()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var logout = await client.Http.PostAsJsonAsync(
            "/api/v1/auth/logout",
            new { refreshToken = client.RefreshToken },
            ApiClient.Json);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refresh = await client.Http.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = client.RefreshToken },
            ApiClient.Json);

        refresh.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [RequiresDockerFact]
    public async Task An_unauthenticated_caller_cannot_reach_a_protected_resource()
    {
        var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/api/v1/users", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [RequiresDockerFact]
    public async Task A_caller_without_the_required_permission_is_forbidden()
    {
        var administrator = await ApiClient.SignInAsAdministratorAsync(fixture);
        var roles = await administrator.Http.GetFromJsonAsync<IReadOnlyList<RoleResponse>>(
            "/api/v1/roles",
            ApiClient.Json);
        var authorRole = roles!.Single(role => role.Name == "Author");

        var password = "Aa1!" + Guid.CreateVersion7().ToString("N")[..12];
        var email = $"author-{Guid.CreateVersion7():N}@itemauthoring.test";
        var created = await administrator.Http.PostAsJsonAsync(
            "/api/v1/users",
            new
            {
                email,
                displayName = "Restricted Author",
                password,
                roleIds = new[] { authorRole.Id },
            },
            ApiClient.Json);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        var author = await ApiClient.SignInAsync(fixture, email, password);
        var forbidden = await author.Http.GetAsync(new Uri("/api/v1/users", UriKind.Relative));

        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await forbidden.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);
        problem!.Code.ShouldBe("auth.forbidden");
    }

    [RequiresDockerFact]
    public async Task The_health_endpoint_is_reachable_without_credentials()
    {
        var anonymous = fixture.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed record CurrentUserResponse(
        Guid Id,
        string Email,
        string DisplayName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);

    private sealed record RoleResponse(Guid Id, string Name);

    private sealed record ProblemResponse(string Code, string Title, string Detail, int Status);
}
