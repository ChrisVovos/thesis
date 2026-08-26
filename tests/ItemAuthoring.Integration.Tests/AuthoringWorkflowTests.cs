using System.Net;
using System.Net.Http.Json;
using ItemAuthoring.Integration.Tests.Infrastructure;
using Shouldly;

namespace ItemAuthoring.Integration.Tests;

/// <summary>
/// Walks an item and an exam through their whole lifecycle against a real database.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthoringWorkflowTests(ApiFixture fixture)
{
    [RequiresDockerFact]
    public async Task An_item_can_be_authored_reviewed_published_and_versioned()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var categoryId = await FirstCategoryIdAsync(client.Http);

        var created = await client.Http.PostAsJsonAsync(
            "/api/v1/items",
            new
            {
                type = "MultipleChoiceSingleResponse",
                stem = "Integration: which of these is a prime number?",
                difficulty = "Easy",
                categoryId,
                maximumScore = 2m,
                options = new[]
                {
                    new { text = "11", isCorrect = true },
                    new { text = "12", isCorrect = false },
                    new { text = "15", isCorrect = false },
                },
            },
            ApiClient.Json);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var itemId = (await created.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        (await client.Http.PostAsync(new Uri($"/api/v1/items/{itemId}/submit", UriKind.Relative), null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.Http.PostAsync(new Uri($"/api/v1/items/{itemId}/approve", UriKind.Relative), null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.Http.PostAsync(new Uri($"/api/v1/items/{itemId}/publish", UriKind.Relative), null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var versions = await client.Http.GetFromJsonAsync<IReadOnlyList<VersionResponse>>(
            $"/api/v1/items/{itemId}/versions",
            ApiClient.Json);

        versions.ShouldNotBeNull().ShouldHaveSingleItem().VersionNumber.ShouldBe(1);
        versions[0].Options.Count.ShouldBe(3);
    }

    [RequiresDockerFact]
    public async Task A_published_item_cannot_be_edited_and_says_why()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var itemId = await FirstPublishedItemIdAsync(client.Http);
        var categoryId = await FirstCategoryIdAsync(client.Http);

        var response = await client.Http.PutAsJsonAsync(
            $"/api/v1/items/{itemId}",
            new
            {
                itemId,
                stem = "Integration: an edit that must be refused.",
                difficulty = "Hard",
                categoryId,
                maximumScore = 3m,
                options = new[]
                {
                    new { text = "A", isCorrect = true },
                    new { text = "B", isCorrect = false },
                },
            },
            ApiClient.Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);
        problem!.Code.ShouldBe("item.not_editable");
    }

    [RequiresDockerFact]
    public async Task An_exam_is_assembled_validated_and_published()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);

        var createdExam = await client.Http.PostAsJsonAsync(
            "/api/v1/exams",
            new
            {
                title = $"Integration exam {Guid.CreateVersion7():N}",
                description = "Assembled by the integration suite.",
                timeLimitMinutes = 60,
                passingScorePercentage = 50,
            },
            ApiClient.Json);
        createdExam.StatusCode.ShouldBe(HttpStatusCode.Created);
        var examId = (await createdExam.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        var publishAttempt = await client.Http.PostAsync(
            new Uri($"/api/v1/exams/{examId}/publish", UriKind.Relative),
            null);
        publishAttempt.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var createdSection = await client.Http.PostAsJsonAsync(
            $"/api/v1/exams/{examId}/sections",
            new { examId, title = "Part A", instructions = "Answer every question." },
            ApiClient.Json);
        createdSection.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sectionId =
            (await createdSection.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        var itemId = await FirstPublishedItemIdAsync(client.Http);
        var placement = await client.Http.PostAsJsonAsync(
            $"/api/v1/exams/{examId}/sections/{sectionId}/items",
            new { examId, sectionId, itemId },
            ApiClient.Json);
        placement.StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.Http.PostAsJsonAsync(
            $"/api/v1/exams/{examId}/sections/{sectionId}/items",
            new { examId, sectionId, itemId },
            ApiClient.Json);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var published = await client.Http.PostAsync(
            new Uri($"/api/v1/exams/{examId}/publish", UriKind.Relative),
            null);
        published.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var detail = await client.Http.GetFromJsonAsync<ExamDetailResponse>(
            $"/api/v1/exams/{examId}",
            ApiClient.Json);
        detail!.Summary.Status.ShouldBe("Published");
        detail.CompositionViolations.ShouldBeEmpty();
        detail.Sections.ShouldHaveSingleItem().Items.ShouldHaveSingleItem();
    }

    [RequiresDockerFact]
    public async Task An_unpublished_item_cannot_be_placed_in_an_exam()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        var categoryId = await FirstCategoryIdAsync(client.Http);

        var createdItem = await client.Http.PostAsJsonAsync(
            "/api/v1/items",
            new
            {
                type = "EitherOr",
                stem = "Integration: a draft that must not be placeable.",
                difficulty = "Easy",
                categoryId,
                maximumScore = 1m,
                options = new[]
                {
                    new { text = "True", isCorrect = true },
                    new { text = "False", isCorrect = false },
                },
            },
            ApiClient.Json);
        var draftItemId =
            (await createdItem.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        var createdExam = await client.Http.PostAsJsonAsync(
            "/api/v1/exams",
            new { title = $"Draft check {Guid.CreateVersion7():N}", passingScorePercentage = 50 },
            ApiClient.Json);
        var examId = (await createdExam.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        var createdSection = await client.Http.PostAsJsonAsync(
            $"/api/v1/exams/{examId}/sections",
            new { examId, title = "Part A", instructions = (string?)null },
            ApiClient.Json);
        var sectionId =
            (await createdSection.Content.ReadFromJsonAsync<CreatedResponse>(ApiClient.Json))!.Id;

        var placement = await client.Http.PostAsJsonAsync(
            $"/api/v1/exams/{examId}/sections/{sectionId}/items",
            new { examId, sectionId, itemId = draftItemId },
            ApiClient.Json);

        placement.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await placement.Content.ReadFromJsonAsync<ProblemResponse>(ApiClient.Json);
        problem!.Code.ShouldBe("exam.item_not_published");
    }

    [RequiresDockerFact]
    public async Task A_graphql_list_query_resolves_navigations_without_an_n_plus_one()
    {
        var client = await ApiClient.SignInAsAdministratorAsync(fixture);
        await client.Http.DeleteAsync(new Uri("/api/v1/benchmark/measurements", UriKind.Relative));

        var response = await ApiClient.GraphQlAsync(
            client.Http,
            """
            query {
              items(first: 20) {
                nodes { id stem category { id name } options { text isCorrect } }
              }
            }
            """);

        response.HasErrors.ShouldBeFalse();
        response.Data.GetProperty("items").GetProperty("nodes").GetArrayLength().ShouldBeGreaterThan(0);

        var measurements = await client.Http.GetFromJsonAsync<IReadOnlyList<MeasurementResponse>>(
            "/api/v1/benchmark/measurements",
            ApiClient.Json);

        var graphqlCall = measurements!.Last(measurement => measurement.Transport == "graphql");

        // One statement for the page, one per batched navigation. Without data loaders this would
        // grow with the number of nodes returned.
        graphqlCall.DatabaseCommands.ShouldBeLessThanOrEqualTo(5);
    }

    private static async Task<Guid> FirstCategoryIdAsync(HttpClient client)
    {
        var categories = await client.GetFromJsonAsync<IReadOnlyList<CategoryResponse>>(
            "/api/v1/categories",
            ApiClient.Json);
        return categories!.First(category => category.IsActive).Id;
    }

    private static async Task<Guid> FirstPublishedItemIdAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResponse<ItemSummaryResponse>>(
            "/api/v1/items?page=1&pageSize=1&status=Published",
            ApiClient.Json);
        return page!.Items[0].Id;
    }

    private sealed record CreatedResponse(Guid Id);

    private sealed record CategoryResponse(Guid Id, string Name, bool IsActive);

    private sealed record ItemSummaryResponse(Guid Id, string Stem, string Status);

    private sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount);

    private sealed record VersionResponse(
        Guid Id,
        int VersionNumber,
        IReadOnlyList<OptionResponse> Options);

    private sealed record OptionResponse(string Text, bool IsCorrect);

    private sealed record ExamSummaryResponse(Guid Id, string Title, string Status);

    private sealed record ExamSectionResponse(Guid Id, IReadOnlyList<ExamItemResponse> Items);

    private sealed record ExamItemResponse(Guid Id, Guid ItemId);

    private sealed record ExamDetailResponse(
        ExamSummaryResponse Summary,
        IReadOnlyList<ExamSectionResponse> Sections,
        IReadOnlyList<string> CompositionViolations);

    private sealed record ProblemResponse(string Code, string Title, string Detail, int Status);

    private sealed record MeasurementResponse(
        string Transport,
        string Operation,
        int DatabaseCommands,
        long ResponseBytes);
}
