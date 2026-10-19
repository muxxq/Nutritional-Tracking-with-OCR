using MediatR;
using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Application.Recipes.Commands.ArchiveRecipe;

/// <summary>"Deleting" archives (ADR-006 D4): the recipe leaves the list, but diary entries logged from it stay.</summary>
public sealed class ArchiveRecipeCommandHandler(IApplicationDbContext db, IDateTimeProvider clock) : IRequestHandler<ArchiveRecipeCommand>
{
    public async Task Handle(ArchiveRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await db.LoadVisibleAsync(request.UserId, request.RecipeId, cancellationToken);
        recipe.Archive(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
