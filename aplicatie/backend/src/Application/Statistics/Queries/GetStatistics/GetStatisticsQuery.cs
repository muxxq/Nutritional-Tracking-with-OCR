using MediatR;
using NutriScan.Application.Statistics.Dtos;

namespace NutriScan.Application.Statistics.Queries.GetStatistics;

/// <summary>Read model for the progress charts: a week, a month or any range up to a year (ADR-009 D1).</summary>
public sealed record GetStatisticsQuery(Guid UserId, DateOnly From, DateOnly To) : IRequest<StatisticsDto>;
