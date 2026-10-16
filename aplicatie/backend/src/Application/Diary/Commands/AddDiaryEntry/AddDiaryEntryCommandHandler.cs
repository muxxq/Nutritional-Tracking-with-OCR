using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Diary.Dtos;
using NutriScan.Application.Foods;
using NutriScan.Application.Recipes;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Diary.Commands.AddDiaryEntry;

public sealed class AddDiaryEntryCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<AddDiaryEntryCommand, DiaryEntryDto>
{
    public async Task<DiaryEntryDto> Handle(AddDiaryEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new DiaryEntry { UserId = request.UserId, CreatedAtUtc = clock.UtcNow };

        // FoodAccess and RecipeAccess share a signature, so they are called by class name here.
        if (request.RecipeId is { } recipeId)
        {
            // Only the user's own recipes that are not archived can be logged.
            var recipe = await RecipeAccess.LoadVisibleAsync(db, request.UserId, recipeId, cancellationToken);
            entry.RecipeId = recipe.Id;
            entry.Recipe = recipe;
            entry.Apply(request, recipe);
        }
        else
        {
            // Only foods the user can see in the catalog can be logged: global or own, not archived.
            var food = await FoodAccess.LoadVisibleAsync(db, request.UserId, request.FoodId!.Value, cancellationToken);
            entry.FoodItemId = food.Id;
            entry.FoodItem = food;
            entry.Apply(request, food);
        }

        db.DiaryEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return DiaryEntryDto.From(entry);
    }
}
