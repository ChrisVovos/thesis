using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ItemAuthoring.Infrastructure.Persistence.Converters;

/// <summary>Converts an <see cref="ItemStem"/> to and from its text column.</summary>
public sealed class ItemStemConverter : ValueConverter<ItemStem, string>
{
    /// <summary>Initializes a new instance of the <see cref="ItemStemConverter"/> class.</summary>
    public ItemStemConverter()
        : base(stem => stem.Text, text => ItemStem.Create(text))
    {
    }
}

/// <summary>Converts an <see cref="OptionText"/> to and from its text column.</summary>
public sealed class OptionTextConverter : ValueConverter<OptionText, string>
{
    /// <summary>Initializes a new instance of the <see cref="OptionTextConverter"/> class.</summary>
    public OptionTextConverter()
        : base(text => text.Text, value => OptionText.Create(value))
    {
    }
}

/// <summary>Converts a <see cref="Points"/> value to and from its decimal column.</summary>
public sealed class PointsConverter : ValueConverter<Points, decimal>
{
    /// <summary>Initializes a new instance of the <see cref="PointsConverter"/> class.</summary>
    public PointsConverter()
        : base(points => points.Value, value => Points.Create(value))
    {
    }
}

/// <summary>Converts a <see cref="CategoryName"/> to and from its text column.</summary>
public sealed class CategoryNameConverter : ValueConverter<CategoryName, string>
{
    /// <summary>Initializes a new instance of the <see cref="CategoryNameConverter"/> class.</summary>
    public CategoryNameConverter()
        : base(name => name.Value, value => CategoryName.Create(value))
    {
    }
}

/// <summary>Converts an <see cref="ExamTitle"/> to and from its text column.</summary>
public sealed class ExamTitleConverter : ValueConverter<ExamTitle, string>
{
    /// <summary>Initializes a new instance of the <see cref="ExamTitleConverter"/> class.</summary>
    public ExamTitleConverter()
        : base(title => title.Value, value => ExamTitle.Create(value))
    {
    }
}
