using FluentAssertions;
using Moq;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Planner.Commands.CreatePlannedMeal;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Planner.Queries.GetPlan;
using NutriScan.Application.Planner.Queries.GetPlannedMeal;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Application.UnitTests.Profile;
using NutriScan.Domain.Entities;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.UnitTests.Planner;

public class GetPlanTests : IDisposable
{
    private static readonly DateOnly Monday = new(2026, 10, 19);

    private readonly AuthTestContext _ctx = new();
    private User _user = null!;
    private FoodItem _food = null!;

    private Guid Me => _user.Id;

    /// <summary>R1 profile (target 1700 kcal) and a food with 100 kcal per 100 g, so grams equal kcal.</summary>
    private async Task ArrangeAsync()
    {
        _user = await _ctx.SeedUserAsync();
        await new UpdateProfileCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(ProfileTestData.R1(Me), CancellationToken.None);
        _food = new FoodItem { Name = "Test", CreatedByUserId = Me, Calories = 100, Proteins = 5, Carbohydrates = 10, Fats = 2 };
        _ctx.Db.FoodItems.Add(_food);
        await _ctx.Db.SaveChangesAsync();
    }

    private async Task<PlannedMealDto> PlanAsync(DateOnly date, double grams, MealType meal = MealType.Lunch, Guid? userId = null)
    {
        var dto = await new CreatePlannedMealCommandHandler(_ctx.Db, _ctx.Clock.Object)
            .Handle(new CreatePlannedMealCommand(userId ?? Me, _food.Id, null, date, meal, grams, null), CancellationToken.None);
        _ctx.Now = _ctx.Now.AddMinutes(1);
        return dto;
    }

    private Task<PlanDto> GetPlan(DateOnly from, DateOnly to) =>
        new GetPlanQueryHandler(_ctx.Db, _ctx.Clock.Object).Handle(new GetPlanQuery(Me, from, to), CancellationToken.None);

    [Fact]
    public async Task GetPlan_IncludesEmptyDays()
    {
        // Arrange
        await ArrangeAsync();
        await PlanAsync(Monday.AddDays(1), 500);

        // Act
        var plan = await GetPlan(Monday, Monday.AddDays(6));

        // Assert
        plan.Days.Select(d => d.Date).Should().Equal(Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)));
        plan.Days.Should().AllSatisfy(d => d.Meals.Select(m => m.MealType).Should().Equal(Enum.GetValues<MealType>()));
        plan.Days[0].Totals.Calories.Should().Be(0);
        plan.Days[0].Status.Should().Be(PlanDayStatus.Under);
        plan.Days[1].Totals.Calories.Should().Be(500);
        plan.IsProfileComplete.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlan_SumsMealsAndDay_StatusAgainstTarget()
    {
        // Arrange
        await ArrangeAsync();
        await PlanAsync(Monday, 600, MealType.Breakfast);
        await PlanAsync(Monday, 1050, MealType.Dinner);
        await PlanAsync(Monday.AddDays(1), 1800);

        // Act
        var plan = await GetPlan(Monday, Monday.AddDays(1));

        // Assert
        var monday = plan.Days[0];
        monday.Meals.Single(m => m.MealType == MealType.Breakfast).Totals.Calories.Should().Be(600);
        monday.Totals.Calories.Should().Be(1650);
        monday.Targets.Calories.Should().Be(1700);
        monday.Status.Should().Be(PlanDayStatus.Within);
        plan.Days[1].Status.Should().Be(PlanDayStatus.Over);
    }

    [Fact]
    public async Task GetPlan_OrdersItemsByCreatedAt()
    {
        // Arrange
        await ArrangeAsync();
        var first = await PlanAsync(Monday, 100);
        var second = await PlanAsync(Monday, 200);

        // Act
        var plan = await GetPlan(Monday, Monday);

        // Assert
        plan.Days.Single().Meals.Single(m => m.MealType == MealType.Lunch).Items.Select(i => i.Id)
            .Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task GetPlan_ExcludesOtherUsersAndOutOfRange()
    {
        // Arrange
        await ArrangeAsync();
        var stranger = await _ctx.SeedUserAsync("mihai@exemplu.ro");
        _food.CreatedByUserId = null;  // global, so the stranger can plan it too
        await _ctx.Db.SaveChangesAsync();
        await PlanAsync(Monday, 300, userId: stranger.Id);
        await PlanAsync(Monday.AddDays(-1), 400);
        await PlanAsync(Monday.AddDays(1), 500);

        // Act
        var plan = await GetPlan(Monday, Monday);

        // Assert
        plan.Days.Single().Totals.Calories.Should().Be(0);
    }

    [Fact]
    public async Task GetPlan_ReflectsCurrentFoodValues()
    {
        // Arrange
        await ArrangeAsync();
        await PlanAsync(Monday, 1000);
        _food.Calories = 150;
        await _ctx.Db.SaveChangesAsync();

        // Act
        var plan = await GetPlan(Monday, Monday);

        // Assert
        plan.Days.Single().Totals.Calories.Should().Be(1500);
    }

    [Fact]
    public async Task GetPlannedMeal_OtherUsers_ThrowsNotFound()
    {
        // Arrange
        await ArrangeAsync();
        var meal = await PlanAsync(Monday, 100);

        // Act
        var act = () => new GetPlannedMealQueryHandler(_ctx.Db)
            .Handle(new GetPlannedMealQuery(Guid.NewGuid(), meal.Id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static GetPlanQueryValidator Validator()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
        return new GetPlanQueryValidator(clock.Object);
    }

    [Fact]
    public void Validate_FromAfterTo_IsInvalid() =>
        Validator().Validate(new GetPlanQuery(Guid.NewGuid(), Monday, Monday.AddDays(-1))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_Range31Days_IsValid() =>
        Validator().Validate(new GetPlanQuery(Guid.NewGuid(), Monday, Monday.AddDays(30))).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_Range32Days_IsInvalid() =>
        Validator().Validate(new GetPlanQuery(Guid.NewGuid(), Monday, Monday.AddDays(31))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_SingleDay_IsValid() =>
        Validator().Validate(new GetPlanQuery(Guid.NewGuid(), Monday, Monday)).IsValid.Should().BeTrue();

    public void Dispose() => _ctx.Dispose();
}
