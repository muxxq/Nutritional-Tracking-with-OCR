using FluentAssertions;
using Moq;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary;
using NutriScan.Application.Diary.Commands.AddDiaryEntry;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public class DiaryEntryFieldsValidatorTests
{
    private readonly DiaryEntryFieldsValidator _validator;

    public DiaryEntryFieldsValidatorTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
        _validator = new DiaryEntryFieldsValidator(clock.Object);
    }

    private static AddDiaryEntryCommand Valid() => DiaryTestData.Add(Guid.NewGuid(), Guid.NewGuid());

    private bool IsValid(AddDiaryEntryCommand command) => _validator.Validate(command).IsValid;

    [Fact]
    public void Validate_Grams_IsValid() => IsValid(Valid()).Should().BeTrue();

    [Fact]
    public void Validate_DateTomorrow_IsValid() => IsValid(Valid() with { Date = new DateOnly(2026, 10, 17) }).Should().BeTrue();

    [Fact]
    public void Validate_DateInTwoDays_IsInvalid() => IsValid(Valid() with { Date = new DateOnly(2026, 10, 18) }).Should().BeFalse();

    [Fact]
    public void Validate_DateBefore2000_IsInvalid() => IsValid(Valid() with { Date = new DateOnly(1999, 12, 31) }).Should().BeFalse();

    [Fact]
    public void Validate_FirstDayOf2000_IsValid() => IsValid(Valid() with { Date = new DateOnly(2000, 1, 1) }).Should().BeTrue();

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(5000.1, false)]
    [InlineData(5000, true)]
    public void Validate_GramsBounds(double grams, bool expected) =>
        IsValid(Valid() with { QuantityGrams = grams }).Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(50.1, false)]
    [InlineData(50, true)]
    public void Validate_ServingsBounds(double servings, bool expected) =>
        IsValid(Valid() with { QuantityGrams = null, Servings = servings }).Should().Be(expected);

    [Fact]
    public void Validate_BothQuantities_IsInvalid() => IsValid(Valid() with { QuantityGrams = 100, Servings = 1 }).Should().BeFalse();

    [Fact]
    public void Validate_NoQuantity_IsInvalid() => IsValid(Valid() with { QuantityGrams = null, Servings = null }).Should().BeFalse();

    [Fact]
    public void Validate_UnknownMealType_IsInvalid() => IsValid(Valid() with { MealType = (MealType)99 }).Should().BeFalse();
}
