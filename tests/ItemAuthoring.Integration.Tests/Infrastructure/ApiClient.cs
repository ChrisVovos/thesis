using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItemAuthoring.Integration.Tests.Infrastructure;

/// <summary>An authenticated client bound to one of the two API surfaces.</summary>
/// <param name="Http">The underlying HTTP client.</param>
/// <param name="AccessToken">The bearer token the client presents.</param>
/// <param name="RefreshToken">The refresh token issued alongside it.</param>
public sealed record AuthenticatedClient(
    HttpClient Http,
    string AccessToken,
    string RefreshToken);

/// <summary>Helpers shared by every integration test.</summary>
public static class ApiClient
{
    /// <summary>The JSON options that mirror the host's serializer configuration.</summary>
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Signs in and returns a client that presents the resulting bearer token.</summary>
    /// <param name="fixture">The shared API fixture.</param>
    /// <param name="email">The login identifier.</param>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The authenticated client.</returns>
    public static async Task<AuthenticatedClient> SignInAsync(
        ApiFixture fixture,
        string email,
        string password)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password },
            Json);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(Json)
            ?? throw new InvalidOperationException("The sign-in response was empty.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.AccessToken);

        return new AuthenticatedClient(client, payload.AccessToken, payload.RefreshToken);
    }

    /// <summary>Signs in as the seeded administrator.</summary>
    /// <param name="fixture">The shared API fixture.</param>
    /// <returns>The authenticated client.</returns>
    public static Task<AuthenticatedClient> SignInAsAdministratorAsync(ApiFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return SignInAsync(fixture, ApiFixture.AdministratorEmail, fixture.AdministratorPassword);
    }

    /// <summary>Executes a GraphQL operation and returns the parsed response.</summary>
    /// <param name="client">The authenticated client.</param>
    /// <param name="query">The GraphQL document.</param>
    /// <param name="variables">The operation variables.</param>
    /// <returns>The parsed GraphQL response.</returns>
    public static async Task<GraphQlResponse> GraphQlAsync(
        HttpClient client,
        string query,
        object? variables = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.PostAsJsonAsync("/graphql", new { query, variables }, Json);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        var data = document.RootElement.TryGetProperty("data", out var dataElement)
            ? dataElement.Clone()
            : default;

        var errors = document.RootElement.TryGetProperty("errors", out var errorsElement)
            ? errorsElement.Clone()
            : default;

        return new GraphQlResponse(response.StatusCode, data, errors);
    }

    private sealed record LoginResponse(string AccessToken, string RefreshToken);
}

/// <summary>The parsed result of a GraphQL request.</summary>
/// <param name="StatusCode">The transport status code.</param>
/// <param name="Data">The <c>data</c> member, when present.</param>
/// <param name="Errors">The <c>errors</c> member, when present.</param>
public sealed record GraphQlResponse(
    System.Net.HttpStatusCode StatusCode,
    JsonElement Data,
    JsonElement Errors)
{
    /// <summary>Gets a value indicating whether the operation reported any error.</summary>
    public bool HasErrors => Errors.ValueKind is JsonValueKind.Array && Errors.GetArrayLength() > 0;

    /// <summary>Reads the stable error code of the first reported error.</summary>
    /// <returns>The error code, or <see langword="null"/> when no error was reported.</returns>
    public string? FirstErrorCode()
        => HasErrors && Errors[0].TryGetProperty("extensions", out var extensions)
            && extensions.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
}
