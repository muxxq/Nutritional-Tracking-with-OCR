using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Diary;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Diary;

public class DiaryReviewFixTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    [Fact]
    public void Rounded_TinyNegative_IsPlainZero()
    {
        // Act: 0.03 kcal over target would otherwise serialize as "-0"
        var rounded = new NutrientsDto(-0.03, -0.04, -0.01, 0).Rounded();

        // Assert
        double.IsNegative(rounded.Calories).Should().BeFalse();
        double.IsNegative(rounded.Proteins).Should().BeFalse();
        double.IsNegative(rounded.Carbs).Should().BeFalse();
    }

    [Fact]
    public async Task SaveOrNotFound_EntryDeletedConcurrently_ThrowsNotFound()
    {
        // Arrange: this request has loaded the entry and is about to change it...
        var food = new FoodItem { Name = "Ovăz", Calories = 389, Proteins = 16.9, Carbohydrates = 66.3, Fats = 6.9 };
        var entry = new DiaryEntry { UserId = Guid.NewGuid(), FoodItem = food, MealType = MealType.Breakfast, QuantityGrams = 100 };
        _ctx.Db.DiaryEntries.Add(entry);
        await _ctx.Db.SaveChangesAsync();
        entry.QuantityGrams = 50;

        // ...while a concurrent request (another tab, a double tap) deletes it and commits first.
        await using (var other = _ctx.CreateContext())
        {
            other.DiaryEntries.Remove(await other.DiaryEntries.SingleAsync());
            await other.SaveChangesAsync();
        }

        // Act
        var act = () => _ctx.Db.SaveOrNotFoundAsync(CancellationToken.None);

        // Assert: 404 like any other missing entry, not a 500
        await act.Should().ThrowAsync<NotFoundException>().WithMessage(DiaryErrors.NotFound);
    }

    public void Dispose() => _ctx.Dispose();
}
