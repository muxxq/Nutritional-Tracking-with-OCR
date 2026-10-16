using MediatR;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Foods;
using NutriScan.Application.Planner.Dtos;
using NutriScan.Application.Recipes;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Planner.Commands.CreatePlannedMeal;

public sealed class CreatePlannedMealCommandHandler(IApplicationDbContext db, IDateTimeProvider clock)
    : IRequestHandler<CreatePlannedMealCommand, PlannedMealDto>
{
    public async Task<PlannedMealDto> Handle(CreatePlannedMealCommand request, CancellationToken cancellationToken)
    {
        var meal = new PlannedMeal { UserId = request.UserId, CreatedAtUtc = clock.UtcNow };

        // Only what the user can see in the catalog or recipe book can be planned; FoodAccess and RecipeAccess
        // share a signature, so they are called by class name.
        if (request.RecipeId is { } recipeId)
        {
            var recipe = await RecipeAccess.LoadVisibleAsync(db, request.UserId, recipeId, cancellationToken);
            meal.RecipeId = recipe.Id;
            meal.Recipe = recipe;
        }
        else
        {
            var food = await FoodAccess.LoadVisibleAsync(db, request.UserId, request.FoodId!.Value, cancellationToken);
            meal.FoodItemId = food.Id;
            meal.FoodItem = food;
        }

        meal.Apply(request);
        db.PlannedMeals.Add(meal);
        await db.SaveChangesAsync(cancellationToken);

        return PlannedMealDto.From(meal);
    }
}
