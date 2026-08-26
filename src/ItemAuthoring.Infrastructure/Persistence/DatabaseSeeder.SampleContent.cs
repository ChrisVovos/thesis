using System.Globalization;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <content>
/// Generation of the sample item bank used by the benchmark harness.
/// </content>
/// <remarks>
/// The generator is deterministic: the same seed produces the same bank on every machine, so a
/// REST run and a GraphQL run measure the same data and the comparison is reproducible.
/// </remarks>
public sealed partial class DatabaseSeeder
{
    private const int RandomSeed = 20_260_824;

    private static readonly string[] SubjectNames =
        ["Mathematics", "Computer Science", "Physics", "Statistics"];

    private static readonly string[] TopicNames =
        ["Foundations", "Applications", "Analysis", "Advanced Topics"];

    private static readonly string[] TagLabels =
        ["algebra", "algorithms", "calculus", "databases", "networking", "probability", "security"];

    private async Task SeedSampleContentAsync(CancellationToken cancellationToken)
    {
        if (await context.Items.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var administratorEmail = EmailAddress.Create(_options.AdministratorEmail).Normalized;
        var author = await context.Users
            .FirstAsync(user => user.Email.Normalized == administratorEmail, cancellationToken);

        var categories = await SeedCategoriesAsync(cancellationToken);
        var tags = await SeedTagsAsync(cancellationToken);
        var random = new Random(RandomSeed);

        for (var index = 0; index < _options.SampleItemCount; index++)
        {
            var category = categories[random.Next(categories.Count)];
            var item = CreateSampleItem(index, random, category, author.Id);
            item.ReplaceTags(PickTags(random, tags));
            PromoteToPublished(item, index);
            context.Items.Add(item);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} sample items.", _options.SampleItemCount);
    }

    private async Task<List<CategoryId>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var leaves = new List<CategoryId>();

        foreach (var subjectName in SubjectNames)
        {
            var subject = Category.Create(
                CategoryName.Create(subjectName),
                $"Assessment content for {subjectName}.");
            context.Categories.Add(subject);

            foreach (var topicName in TopicNames)
            {
                var topic = Category.Create(
                    CategoryName.Create(topicName),
                    $"{topicName} within {subjectName}.",
                    subject.Id);
                context.Categories.Add(topic);
                leaves.Add(topic.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return leaves;
    }

    private async Task<List<TagId>> SeedTagsAsync(CancellationToken cancellationToken)
    {
        var tags = TagLabels.Select(label => Tag.Create(TagName.Create(label))).ToList();
        context.Tags.AddRange(tags);
        await context.SaveChangesAsync(cancellationToken);
        return tags.ConvertAll(tag => tag.Id);
    }

    private static List<TagId> PickTags(Random random, List<TagId> tags)
        => tags.OrderBy(_ => random.Next()).Take(random.Next(1, 4)).ToList();

    private static Item CreateSampleItem(int index, Random random, CategoryId category, UserId author)
    {
        var number = (index + 1).ToString(CultureInfo.InvariantCulture);
        var difficulty = (DifficultyLevel)random.Next(1, 6);
        var score = Points.Create(random.Next(1, 6));
        var stem = ItemStem.Create($"Sample question {number}: which statement is correct?");

        return (index % 4) switch
        {
            0 => SingleResponseItem.Create(stem, difficulty, category, score, author,
            [
                ItemOption.Create("The first statement.", true, 0, "This is the correct reading."),
                ItemOption.Create("The second statement.", false, 1),
                ItemOption.Create("The third statement.", false, 2),
                ItemOption.Create("The fourth statement.", false, 3),
            ]),
            1 => MultipleResponseItem.Create(stem, difficulty, category, score, author,
            [
                ItemOption.Create("The first statement.", true, 0),
                ItemOption.Create("The second statement.", true, 1),
                ItemOption.Create("The third statement.", false, 2),
                ItemOption.Create("The fourth statement.", false, 3),
            ]),
            2 => EitherOrItem.Create(
                ItemStem.Create($"Sample assertion {number}: the statement below is sound."),
                difficulty,
                category,
                score,
                author,
                "True",
                "False",
                index % 8 == 2),
            _ => EssayItem.Create(
                ItemStem.Create($"Sample prompt {number}: explain the concept in your own words."),
                difficulty,
                category,
                score,
                author,
                EssayRubric.Create(
                    "Award marks for a correct definition, a worked example and a stated limitation.",
                    120,
                    400),
                "A complete answer defines the concept, illustrates it and names one limitation."),
        };
    }

    private static void PromoteToPublished(Item item, int index)
    {
        // Roughly three quarters of the bank is published so the exam builder has material to work
        // with, while the remainder exercises every other lifecycle state.
        switch (index % 8)
        {
            case 0:
                return;

            case 1:
                item.SubmitForReview();
                return;

            case 2:
                item.SubmitForReview();
                item.Approve();
                return;

            default:
                item.SubmitForReview();
                item.Approve();
                item.Publish(DateTimeOffset.UtcNow);
                return;
        }
    }
}
