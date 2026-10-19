using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Recipes.Dtos;

namespace NutriScan.Application.Recipes.Commands.UpdateRecipe;

/// <summary>
/// Replaces the fields and the ingredient list in one SaveChanges, hence one transaction (ADR-006 D5).
/// Last write wins (D7). Diary entries keep their snapshot; only the recipe's computed values change.
/// </summary>
public sealed class UpdateRecipeCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await db.LoadVisibleAsync(request.UserId, request.RecipeId, cancellationToken);
        await recipe.ApplyAsync(db, request.UserId, request, cancellationToken);
        recipe.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return RecipeDto.From(recipe);
    }
}
