using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Profile;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Queries.GetDiaryDay;

public sealed class GetDiaryDayQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GetDiaryDayQuery, DiaryDayDto>
{
    public async Task<DiaryDayDto> Handle(GetDiaryDayQuery request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);

        var entries = await db.DiaryEntries
            .Include(d => d.FoodItem)
            .Where(d => d.UserId == request.UserId && d.ConsumedDate == request.Date)
            .OrderBy(d => d.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        // Sums use the unrounded snapshots; rounding happens once, when the DTO is built.
        var meals = Enum.GetValues<MealType>()
            .Select(meal => entries.Where(e => e.MealType == meal).ToList())
            .ToList();
        var mealTotals = meals.Select(m => m.Aggregate(NutrientsDto.Zero, (sum, e) => sum + NutrientsDto.Of(e))).ToList();
        var dayTotal = mealTotals.Aggregate(NutrientsDto.Zero, (sum, t) => sum + t);

        // Past days are compared with today's targets: target history is not stored (spec D5).
        var t = ProfileLoader.CalculateTargets(user, DateOnly.FromDateTime(clock.UtcNow));
        var targets = new NutrientsDto(t.Calories.Value, t.ProteinsGrams.Value, t.CarbsGrams.Value, t.FatsGrams.Value);

        return new DiaryDayDto(
            request.Date,
            Enum.GetValues<MealType>()
                .Select((meal, i) => new DiaryMealDto(
                    meal, meals[i].Select(e => DiaryEntryDto.From(e, e.FoodItem!)).ToList(), mealTotals[i].Rounded()))
                .ToList(),
            dayTotal.Rounded(),
            targets.Rounded(),
            // From the displayed values, so totals + remaining always add up to the target on screen.
            (targets.Rounded() - dayTotal.Rounded()).Rounded(),
            user.IsProfileComplete);
    }
}
