using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Profile;
using NutriScan.Domain.Enums;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Planner.Queries.GetPlan;

/// <summary>
/// Values are computed now, from current foods and recipes (ADR-007 D2). Every day of the range is returned, empty or not,
/// so the client draws the week without filling gaps. Sums use unrounded values and are rounded once, in the DTO.
/// </summary>
public sealed class GetPlanQueryHandler(IApplicationDbContext db, IDateTimeProvider clock) : IRequestHandler<GetPlanQuery, PlanDto>
{
    public async Task<PlanDto> Handle(GetPlanQuery request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);

        var meals = await db.PlannedMeals
            .WithItem()
            .Where(m => m.UserId == request.UserId && m.PlanDate >= request.From && m.PlanDate <= request.To)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        // Today's targets for every day, as in the diary (ADR-005 D5).
        var t = ProfileLoader.CalculateTargets(user, DateOnly.FromDateTime(clock.UtcNow));
        var targets = new NutrientsDto(t.Calories.Value, t.ProteinsGrams.Value, t.CarbsGrams.Value, t.FatsGrams.Value);

        var days = Enumerable.Range(0, request.To.DayNumber - request.From.DayNumber + 1)
            .Select(offset =>
            {
                var date = request.From.AddDays(offset);
                var mealsOfDay = Enum.GetValues<MealType>()
                    .Select(type =>
                    {
                        var items = meals.Where(m => m.PlanDate == date && m.MealType == type).ToList();
                        var total = items.Aggregate(NutrientsDto.Zero, (sum, m) => sum + NutrientsDto.From(m.Nutrients()));
                        return (Type: type, Items: items, Total: total);
                    })
                    .ToList();
                var dayTotal = mealsOfDay.Aggregate(NutrientsDto.Zero, (sum, m) => sum + m.Total);

                return new PlanDayDto(
                    date,
                    mealsOfDay.Select(m => new PlanMealDto(m.Type, m.Items.Select(PlannedMealDto.From).ToList(), m.Total.Rounded())).ToList(),
                    dayTotal.Rounded(),
                    targets.Rounded(),
                    PlanDayStatusCalculator.Of(dayTotal.Calories, targets.Calories));
            })
            .ToList();

        return new PlanDto(request.From, request.To, days, user.IsProfileComplete);
    }
}
