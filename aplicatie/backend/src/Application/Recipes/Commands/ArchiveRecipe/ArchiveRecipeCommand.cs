using MediatR;

namespace NutriScan.Application.Recipes.Commands.ArchiveRecipe;

public sealed record ArchiveRecipeCommand(Guid UserId, Guid RecipeId) : IRequest;
