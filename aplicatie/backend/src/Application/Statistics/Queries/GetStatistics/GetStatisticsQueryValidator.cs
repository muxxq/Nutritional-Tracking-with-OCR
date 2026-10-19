using FluentValidation;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Statistics.Queries.GetStatistics;

public sealed class GetStatisticsQueryValidator : AbstractValidator<GetStatisticsQuery>
{
    /// <summary>A year of daily points: enough for month-by-month trends, still one small row per day.</summary>
    public const int MaxDays = 366;

    private static readonly DateOnly EarliestDate = new(2000, 1, 1);

    public GetStatisticsQueryValidator(IDateTimeProvider clock)
    {
        // The current week or month may end in the future; those days are simply not logged yet.
        var latest = () => DateOnly.FromDateTime(clock.UtcNow).AddDays(366);

        RuleFor(q => q.From)
            .Must(d => d >= EarliestDate && d <= latest())
            .WithMessage(StatisticsErrors.DateRange);
        RuleFor(q => q.To)
            .Must(d => d >= EarliestDate && d <= latest())
            .WithMessage(StatisticsErrors.DateRange);
        RuleFor(q => q)
            .Must(q => q.From <= q.To && q.To.DayNumber - q.From.DayNumber + 1 <= MaxDays)
            .OverridePropertyName("Range")
            .WithMessage(StatisticsErrors.RangeLimits);
    }
}
