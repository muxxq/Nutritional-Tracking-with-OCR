using MediatR;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Diary.Queries.GetDiaryDay;

/// <summary>Read model for the dashboard: the day's four meals with totals against the user's targets.</summary>
public sealed record GetDiaryDayQuery(Guid UserId, DateOnly Date) : IRequest<DiaryDayDto>;
