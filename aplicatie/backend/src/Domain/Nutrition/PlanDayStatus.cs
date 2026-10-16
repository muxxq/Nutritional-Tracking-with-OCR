namespace NutriScan.Domain.Nutrition;

public enum PlanDayStatus { Under, Within, Over }

/// <summary>FR-10: a planned day's energy against the calorie target, within ±5% counting as on target.</summary>
public static class PlanDayStatusCalculator
{
    public const double Tolerance = 0.05;

    public static PlanDayStatus Of(double totalKcal, double targetKcal) =>
        totalKcal < targetKcal * (1 - Tolerance) ? PlanDayStatus.Under
        : totalKcal > targetKcal * (1 + Tolerance) ? PlanDayStatus.Over
        : PlanDayStatus.Within;
}
