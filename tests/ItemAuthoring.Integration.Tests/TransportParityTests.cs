using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ItemAuthoring.Integration.Tests.Infrastructure;
using Shouldly;

namespace ItemAuthoring.Integration.Tests;

/// <summary>
/// Verifies that the two API surfaces agree on outcomes, error codes and authorization.
/// </summary>
/// <remarks>
/// These are the tests that justify the architecture: every one of them performs the same logical
/// operation twice, once per transport, and asserts that the observable result is the same. If the
/// business logic were duplicated in either surface, a test here would fail long before the
/// measurements were taken.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class TransportParityTests(ApiFixture fixture)
{
    [RequiresDockerFact]
    public async Task Both_surfaces_return_the_same_item_for_the_same_identifier()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var itemId = await FirstItemIdAsync(client.Http);

        var rest = await client.Http.GetFromJsonAsync<ItemDetailResponse>(
            $"/api/v1/items/{itemId}",
            ApiClient.Json);

        var graphql = await ApiClient.GraphQlAsync(
            client.Http,
            """
            query ItemById($id: UUID!) {
              itemById(id: $id) {
                summary { id stem status type maximumScore }
              }
            }
            """,
            new { id = itemId });

        graphql.HasErrors.ShouldBeFalse();
        var summary = graphql.Data.GetProperty("itemById").GetProperty("summary");
        summary.GetProperty("id").GetString().ShouldBe(rest!.Summary.Id.ToString());
        summary.GetProperty("stem").GetString().ShouldBe(rest.Summary.Stem);
        summary.GetProperty("status").GetString()
            .ShouldBe(rest.Summary.Status.ToUpperInvariant());
    }

    [RequiresDockerFact]
    public async Task Both_surfaces_report_a_missing_item_with_the_same_code()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var unknownId = Guid.CreateVersion7();

        var rest = await client.Http.GetAsync(new Uri($"/api/v1/items/{unknownId}", UriKind.Relative));
        var restProblem = await rest.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);

        var graphql = await ApiClient.GraphQlAsync(
            client.Http,
            "query ItemById($id: UUID!) { itemById(id: $id) { summary { id } } }",
            new { id = unknownId });

        rest.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        restProblem!.Code.ShouldBe("item.not_found");
        graphql.FirstErrorCode().ShouldBe("item.not_found");
    }

    [RequiresDockerFact]
    public async Task Both_surfaces_deny_an_unauthenticated_caller()
    {
        var anonymous = fixture.CreateClient();

        var rest = await anonymous.GetAsync(new Uri("/api/v1/items", UriKind.Relative));
        var graphql = await ApiClient.GraphQlAsync(
            anonymous,
            "query { items(first: 1) { nodes { id } } }");

        rest.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        graphql.FirstErrorCode().ShouldBe("auth.required");
    }

    [RequiresDockerFact]
    public async Task Both_surfaces_reject_the_same_invalid_input()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var invalid = new
        {
            type = "MULTIPLE_CHOICE_SINGLE_RESPONSE",
            stem = "",
            difficulty = "EASY",
            categoryId = Guid.CreateVersion7(),
            maximumScore = 1m,
            options = new[] { new { text = "A", isCorrect = true, feedback = (string?)null } },
        };

        var rest = await client.Http.PostAsJsonAsync("/api/v1/items", invalid, ApiClient.Json);
        var restProblem = await rest.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);

        var graphql = await ApiClient.GraphQlAsync(
            client.Http,
            """
            mutation Create($input: CreateItemCommandInput!) { createItem(input: $input) }
            """,
            new { input = invalid });

        rest.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        restProblem!.Code.ShouldBe("validation.failed");
        graphql.FirstErrorCode().ShouldBe("validation.failed");
    }

    [RequiresDockerFact]
    public async Task Both_surfaces_report_the_same_total_for_the_same_search()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var rest = await client.Http.GetFromJsonAsync<PagedResponse<ItemSummaryResponse>>(
            "/api/v1/items?page=1&pageSize=5&status=Published",
            ApiClient.Json);

        var graphql = await ApiClient.GraphQlAsync(
            client.Http,
            """
            query Search($criteria: ItemSearchCriteriaInput!) {
              searchItems(criteria: $criteria) { totalCount items { id } }
            }
            """,
            new { criteria = new { page = 1, pageSize = 5, statuses = new[] { "PUBLISHED" } } });

        graphql.HasErrors.ShouldBeFalse();
        var payload = graphql.Data.GetProperty("searchItems");
        payload.GetProperty("totalCount").GetInt32().ShouldBe(rest!.TotalCount);
        payload.GetProperty("items").GetArrayLength().ShouldBe(rest.Items.Count);
    }

    [RequiresDockerFact]
    public async Task A_conditional_get_avoids_resending_an_unchanged_item()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var itemId = await FirstItemIdAsync(client.Http);

        var first = await client.Http.GetAsync(new Uri($"/api/v1/items/{itemId}", UriKind.Relative));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var entityTag = first.Headers.ETag.ShouldNotBeNull();

        using var conditional = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"/api/v1/items/{itemId}", UriKind.Relative));
        conditional.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(entityTag.ToString()));

        var second = await client.Http.SendAsync(conditional);

        second.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [RequiresDockerFact]
    public async Task A_correlation_identifier_is_echoed_by_both_surfaces()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var rest = await client.Http.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));
        var graphql = await client.Http.PostAsJsonAsync(
            "/graphql",
            new { query = "query { categories { id } }" },
            ApiClient.Json);

        rest.Headers.Contains("X-Correlation-Id").ShouldBeTrue();
        graphql.Headers.Contains("X-Correlation-Id").ShouldBeTrue();
    }

    private static async Task<Guid> FirstItemIdAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<ItemSummaryResponse>>(
            "/api/v1/items?page=1&pageSize=1",
            ApiClient.Json);
        return page!.Items[0].Id;
    }

    private sealed record ItemSummaryResponse(Guid Id, string Stem, string Status);

    private sealed record ItemDetailResponse(ItemSummaryResponse Summary);

    private sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount);

    private sealed record ProblemResponse(string Code, string Title, string Detail, int Status);
}
