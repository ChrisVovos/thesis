using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Domain.Tests;

/// <summary>
/// Builders that keep the tests focused on the behaviour under test instead of on construction noise.
/// </summary>
internal static class TestItems
{
    public static readonly UserId Author = UserId.New();

    public static readonly CategoryId Category = CategoryId.New();

    public static ItemOption Option(string text, bool isCorrect, int position)
        => ItemOption.Create(text, isCorrect, position);

    public static SingleResponseItem SingleResponse()
        => SingleResponseItem.Create(
            ItemStem.Create("Which of the following is a prime number?"),
            DifficultyLevel.Easy,
            Category,
            Points.Create(1m),
            Author,
            [
                Option("7", true, 0),
                Option("8", false, 1),
                Option("9", false, 2),
            ]);

    public static MultipleResponseItem MultipleResponse()
        => MultipleResponseItem.Create(
            ItemStem.Create("Select every prime number."),
            DifficultyLevel.Medium,
            Category,
            Points.Create(2m),
            Author,
            [
                Option("2", true, 0),
                Option("3", true, 1),
                Option("4", false, 2),
            ]);

    public static EitherOrItem EitherOr()
        => EitherOrItem.Create(
            ItemStem.Create("Every prime number greater than two is odd."),
            DifficultyLevel.Easy,
            Category,
            Points.Create(1m),
            Author,
            "True",
            "False",
            positiveIsCorrect: true);

    public static EssayItem Essay()
        => EssayItem.Create(
            ItemStem.Create("Explain why there are infinitely many prime numbers."),
            DifficultyLevel.Hard,
            Category,
            Points.Create(10m),
            Author,
            EssayRubric.Create("Award marks for a correct proof sketch.", 100, 400));

    public static Item Published(Item item)
    {
        item.SubmitForReview();
        item.Approve();
        item.Publish(DateTimeOffset.UtcNow);
        return item;
    }
}
