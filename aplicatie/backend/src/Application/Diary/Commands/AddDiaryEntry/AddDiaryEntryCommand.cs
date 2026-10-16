using MediatR;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

public sealed record AddDiaryEntryCommand(
    Guid UserId, Guid FoodId, DateOnly Date, MealType MealType,
    double? QuantityGrams, double? Servings) : IRequest<DiaryEntryDto>, IDiaryEntryFields;
