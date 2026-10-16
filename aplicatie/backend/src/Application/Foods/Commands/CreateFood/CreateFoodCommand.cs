using MediatR;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Commands.CreateFood;

public sealed record CreateFoodCommand(
    Guid UserId, string Name, string? Brand, string? Barcode, double? ServingSizeGrams,
    double Calories, double Proteins, double Carbohydrates, double? Sugars,
    double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams) : IRequest<FoodDto>, IFoodFields;
