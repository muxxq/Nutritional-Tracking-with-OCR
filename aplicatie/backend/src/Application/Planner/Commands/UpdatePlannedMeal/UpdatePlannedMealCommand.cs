using MediatR;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Planner.Commands.UpdatePlannedMeal;

/// <summary>Changes date, meal and quantity. The food or recipe stays the same: plan another one as a new meal.</summary>
public sealed record UpdatePlannedMealCommand(
    Guid UserId, Guid PlannedMealId, DateOnly Date, MealType MealType,
    double? QuantityGrams, double? Servings) : IRequest<PlannedMealDto>, IPlannedMealFields;
