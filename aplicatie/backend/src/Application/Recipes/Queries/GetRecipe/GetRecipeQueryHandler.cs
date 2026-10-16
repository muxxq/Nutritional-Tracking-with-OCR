using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Queries.GetRecipe;

/// <summary>Values are computed now, from the ingredients' current foods (ADR-006 D1).</summary>
public sealed class GetRecipeQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRecipeQuery, RecipeDto>
{
    public async Task<RecipeDto> Handle(GetRecipeQuery request, CancellationToken cancellationToken) =>
        RecipeDto.From(await db.LoadVisibleAsync(request.UserId, request.RecipeId, cancellationToken));
}
