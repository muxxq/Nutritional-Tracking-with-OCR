using MediatR;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Planner.Commands.CreatePlannedMeal;

/// <summary>Plans exactly one of a food (in grams or servings) or a recipe (in servings only).</summary>
public sealed record CreatePlannedMealCommand(
    Guid UserId, Guid? FoodId, Guid? RecipeId, DateOnly Date, MealType MealType,
    double? QuantityGrams, double? Servings) : IRequest<PlannedMealDto>, IPlannedMealFields;
