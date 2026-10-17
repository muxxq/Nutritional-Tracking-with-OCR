namespace NutriScan.Domain.Nutrition;

public sealed record DayStatusCounts(int Under, int Within, int Over);

/// <summary>
/// Summary of a range of diary days. <see cref="Average"/> and <see cref="Deviation"/> are null when no day was logged.
/// </summary>
public sealed record NutritionSummary(
    int LoggedDays, Macros? Average, Macros? Deviation, double? CaloriesDeviationPercent, DayStatusCounts StatusCounts);

/// <summary>FR-08: averages and deviation from the target over the days the user actually logged (ADR-009 D2–D4).</summary>
public static class NutritionStatistics
{
    /// <param name="loggedDayTotals">One unrounded total per day with at least one entry; empty days are left out.</param>
    public static NutritionSummary Summarize(IReadOnlyCollection<Macros> loggedDayTotals, Macros target)
    {
        var statuses = loggedDayTotals.Select(d => PlanDayStatusCalculator.Of(d.Calories, target.Calories)).ToList();
        var counts = new DayStatusCounts(
            statuses.Count(s => s == PlanDayStatus.Under),
            statuses.Count(s => s == PlanDayStatus.Within),
            statuses.Count(s => s == PlanDayStatus.Over));

        // A day without entries means "not logged", not "ate nothing": averaging it in would show a false deficit.
        if (loggedDayTotals.Count == 0)
        {
            return new NutritionSummary(0, null, null, null, counts);
        }

        var average = loggedDayTotals.Aggregate(Macros.Zero, (sum, d) => sum + d) / loggedDayTotals.Count;
        var deviation = average - target;
        double? percent = target.Calories > 0 ? deviation.Calories / target.Calories * 100 : null;

        return new NutritionSummary(loggedDayTotals.Count, average, deviation, percent, counts);
    }
}
