using MediatR;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Queries.GetRecipe;

public sealed record GetRecipeQuery(Guid UserId, Guid RecipeId) : IRequest<RecipeDto>;
