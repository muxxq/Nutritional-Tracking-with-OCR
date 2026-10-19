using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Common.Models;
using NutriScan.Application.Profile;
using NutriScan.Application.Statistics.Dtos;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Statistics.Queries.GetStatistics;

/// <summary>
/// Sums each day in PostgreSQL (GROUP BY on the (UserId, ConsumedDate) index), so a year is at most 366 small rows
/// instead of every entry with its food (ADR-009 D6). The statistics themselves are a Domain function (D8).
/// </summary>
public sealed class GetStatisticsQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GetStatisticsQuery, StatisticsDto>
{
    public async Task<StatisticsDto> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var user = await db.LoadUserAsync(request.UserId, cancellationToken);

        var logged = await db.DiaryEntries
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId && e.ConsumedDate >= request.From && e.ConsumedDate <= request.To)
            .GroupBy(e => e.ConsumedDate)
            .Select(g => new
            {
                Date = g.Key,
                Calories = g.Sum(e => e.CalculatedCalories),
                Proteins = g.Sum(e => e.CalculatedProteins),
                Carbs = g.Sum(e => e.CalculatedCarbs),
                Fats = g.Sum(e => e.CalculatedFats)
            })
            .ToDictionaryAsync(d => d.Date, d => new Macros(d.Calories, d.Proteins, d.Carbs, d.Fats), cancellationToken);

        // Today's targets for every day, as in the diary and the plan (ADR-005 D5).
        var t = ProfileLoader.CalculateTargets(user, DateOnly.FromDateTime(clock.UtcNow));
        var target = new Macros(t.Calories.Value, t.ProteinsGrams.Value, t.CarbsGrams.Value, t.FatsGrams.Value);
        var targets = NutrientsDto.From(target).Rounded();

        var summary = NutritionStatistics.Summarize(logged.Values, target);

        var totalDays = request.To.DayNumber - request.From.DayNumber + 1;
        var days = Enumerable.Range(0, totalDays)
            .Select(offset =>
            {
                var date = request.From.AddDays(offset);
                return logged.TryGetValue(date, out var total)
                    ? new StatisticsDayDto(date, true, NutrientsDto.From(total).Rounded(),
                        PlanDayStatusCalculator.Of(total.Calories, target.Calories))
                    : new StatisticsDayDto(date, false, NutrientsDto.Zero, null);
            })
            .ToList();

        var average = summary.Average is { } a ? NutrientsDto.From(a).Rounded() : null;

        return new StatisticsDto(
            request.From, request.To, totalDays, summary.LoggedDays,
            targets,
            average,
            // From the displayed values, so average − deviation is exactly the target on screen (as "remaining" in the diary).
            average is null ? null : (average - targets).Rounded(),
            summary.CaloriesDeviationPercent is { } p ? Math.Round(p, 1, MidpointRounding.AwayFromZero) + 0.0 : null,
            summary.StatusCounts,
            days,
            user.IsProfileComplete);
    }
}
