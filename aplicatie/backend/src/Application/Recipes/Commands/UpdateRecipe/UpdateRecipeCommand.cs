using MediatR;
using NutriScan.Application.Recipes.Commands.CreateRecipe;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Commands.UpdateRecipe;

public sealed record UpdateRecipeCommand(
    Guid UserId, Guid RecipeId, string Name, string? Description, string? Instructions, int ServingsCount,
    int? PrepTimeMinutes, IReadOnlyList<RecipeIngredientInput> Ingredients) : IRequest<RecipeDto>, IRecipeFields
{
    public static UpdateRecipeCommand From(Guid recipeId, CreateRecipeCommand c) => new(
        c.UserId, recipeId, c.Name, c.Description, c.Instructions, c.ServingsCount, c.PrepTimeMinutes, c.Ingredients);
}
