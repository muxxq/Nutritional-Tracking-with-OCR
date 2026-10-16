using MediatR;
using NutriScan.Application.Foods.Commands.CreateFood;
using NutriScan.Application.Foods.Dtos;

namespace NutriScan.Application.Foods.Commands.UpdateFood;

public sealed record UpdateFoodCommand(
    Guid UserId, Guid FoodId, string Name, string? Brand, string? Barcode, double? ServingSizeGrams,
    double Calories, double Proteins, double Carbohydrates, double? Sugars,
    double Fats, double? SaturatedFats, double? Fiber, double? SaltGrams) : IRequest<FoodDto>, IFoodFields
{
    public static UpdateFoodCommand From(Guid foodId, CreateFoodCommand c) => new(
        c.UserId, foodId, c.Name, c.Brand, c.Barcode, c.ServingSizeGrams, c.Calories, c.Proteins,
        c.Carbohydrates, c.Sugars, c.Fats, c.SaturatedFats, c.Fiber, c.SaltGrams);
}
