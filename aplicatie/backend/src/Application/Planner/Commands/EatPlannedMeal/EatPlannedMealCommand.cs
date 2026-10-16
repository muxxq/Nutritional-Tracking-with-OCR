using MediatR;
using NutriScan.Application.Diary.Dtos;

namespace NutriScan.Application.Planner.Commands.EatPlannedMeal;

/// <summary>FR-11, "I ate this": logs a planned meal in the diary in one action.</summary>
public sealed record EatPlannedMealCommand(Guid UserId, Guid PlannedMealId) : IRequest<DiaryEntryDto>;
