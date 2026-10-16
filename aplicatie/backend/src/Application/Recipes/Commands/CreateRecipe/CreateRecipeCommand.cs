using MediatR;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Commands.CreateRecipe;

public sealed record CreateRecipeCommand(
    Guid UserId, string Name, string? Description, string? Instructions, int ServingsCount, int? PrepTimeMinutes,
    IReadOnlyList<RecipeIngredientInput> Ingredients) : IRequest<RecipeDto>, IRecipeFields;
