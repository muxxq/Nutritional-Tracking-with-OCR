using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Recipes.Dtos;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        // Recipes are private: only the owner sees them, and only from foods the owner can see.
        var recipe = new Recipe { UserId = request.UserId, CreatedAtUtc = clock.UtcNow };
        await recipe.ApplyAsync(db, request.UserId, request, cancellationToken);

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken);

        return RecipeDto.From(recipe);
    }
}
