using NutriScan.Application.Common.Models;
using NutriScan.Domain.Nutrition;

namespace NutriScan.Application.Statistics.Dtos;

/// <summary>A day of the range; <see cref="Status"/> is null when nothing was logged that day.</summary>
public sealed record StatisticsDayDto(DateOnly Date, bool IsLogged, NutrientsDto Totals, PlanDayStatus? Status);

/// <summary>
/// FR-08 over a range. <see cref="Average"/>, <see cref="Deviation"/> and <see cref="CaloriesDeviationPercent"/>
/// cover logged days only and are null when there are none (ADR-009 D2).
/// </summary>
public sealed record StatisticsDto(
    DateOnly From, DateOnly To, int TotalDays, int LoggedDays,
    NutrientsDto Targets, NutrientsDto? Average, NutrientsDto? Deviation, double? CaloriesDeviationPercent,
    DayStatusCounts StatusCounts, IReadOnlyList<StatisticsDayDto> Days, bool IsProfileComplete);
