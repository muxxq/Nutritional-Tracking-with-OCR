using MediatR;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.Diary.Commands.UpdateDiaryEntry;

/// <summary>Changes quantity, meal and date. The food stays the same: log another food as a new entry.</summary>
public sealed record UpdateDiaryEntryCommand(
    Guid UserId, Guid EntryId, DateOnly Date, MealType MealType,
    double? QuantityGrams, double? Servings) : IRequest<DiaryEntryDto>, IDiaryEntryFields;
