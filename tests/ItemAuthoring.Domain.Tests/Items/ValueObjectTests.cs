using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;
using Shouldly;

namespace ItemAuthoring.Domain.Tests.Items;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_stem_cannot_be_blank(string? text)
        => Should.Throw<DomainException>(() => ItemStem.Create(text))
            .Code.ShouldBe("item.stem_required");

    [Fact]
    public void A_stem_is_trimmed()
        => ItemStem.Create("  Prompt  ").Text.ShouldBe("Prompt");

    [Fact]
    public void A_stem_has_an_upper_length_bound()
        => Should.Throw<DomainException>(() => ItemStem.Create(new string('x', ItemStem.MaxLength + 1)))
            .Code.ShouldBe("item.stem_too_long");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Points_must_be_positive(decimal value)
        => Should.Throw<DomainException>(() => Points.Create(value))
            .Code.ShouldBe("item.points_not_positive");

    [Fact]
    public void Points_are_limited_to_two_decimal_places()
        => Should.Throw<DomainException>(() => Points.Create(1.234m))
            .Code.ShouldBe("item.points_precision");

    [Fact]
    public void Points_have_an_upper_bound()
        => Should.Throw<DomainException>(() => Points.Create(Points.MaxValue + 1m))
            .Code.ShouldBe("item.points_out_of_range");

    [Fact]
    public void Points_add_to_a_new_value()
        => (Points.Create(1.5m) + Points.Create(2.25m)).Value.ShouldBe(3.75m);

    [Fact]
    public void A_tag_records_both_the_typed_and_the_normalized_label()
    {
        var name = TagName.Create("  Algebra ");

        name.Value.ShouldBe("Algebra");
        name.Normalized.ShouldBe("algebra");
    }

    [Fact]
    public void Tags_that_differ_only_in_case_normalize_to_the_same_value()
        => TagName.Create("ALGEBRA").Normalized.ShouldBe(TagName.Create("algebra").Normalized);

    [Fact]
    public void Value_objects_compare_by_value()
        => ItemStem.Create("Prompt").ShouldBe(ItemStem.Create("Prompt"));

    [Fact]
    public void A_category_cannot_be_its_own_parent()
    {
        var category = Category.Create(CategoryName.Create("Mathematics"));

        Should.Throw<DomainException>(() => category.MoveTo(category.Id))
            .Code.ShouldBe("category.self_parent");
    }

    [Fact]
    public void Identifiers_of_different_aggregates_are_not_interchangeable()
    {
        var itemId = ItemId.New();
        var categoryId = CategoryId.From(itemId.Value);

        // The compiler rejects `itemId == categoryId`; the underlying values may still coincide.
        categoryId.Value.ShouldBe(itemId.Value);
        itemId.ShouldNotBe(ItemId.New());
    }
}
