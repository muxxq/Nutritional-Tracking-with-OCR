using MediatR;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

/// <summary>Logs exactly one of a food (in grams or servings) or a recipe (in servings only, ADR-006 D3).</summary>
public sealed record AddDiaryEntryCommand(
    Guid UserId, Guid? FoodId, Guid? RecipeId, DateOnly Date, MealType MealType,
    double? QuantityGrams, double? Servings) : IRequest<DiaryEntryDto>, IDiaryEntryFields;
