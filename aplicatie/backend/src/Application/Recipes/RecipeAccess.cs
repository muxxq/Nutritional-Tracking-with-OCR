using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Application.Foods;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Recipes;

internal static class RecipeAccess
{
    /// <summary>Loads the user's recipe if it is not archived, with its ingredients' foods; otherwise not found.</summary>
    public static async Task<Recipe> LoadVisibleAsync(this IApplicationDbContext db, Guid userId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await db.LoadOwnAsync(userId, recipeId, cancellationToken);
        return recipe.IsVisibleTo(userId) ? recipe : throw new NotFoundException(RecipeErrors.NotFound);
    }

    /// <summary>Loads the user's recipe even if archived: diary entries logged from it stay editable.</summary>
    public static async Task<Recipe> LoadOwnAsync(this IApplicationDbContext db, Guid userId, Guid recipeId, CancellationToken cancellationToken) =>
        await db.Recipes
            .Include(r => r.Ingredients).ThenInclude(i => i.FoodItem)
            .SingleOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, cancellationToken)
        ?? throw new NotFoundException(RecipeErrors.NotFound);

    /// <summary>
    /// Sets the fields and replaces the ingredient list. A food must be visible to the user, unless it already was
    /// an ingredient (an own food archived after it was added); the first one that isn't is reported as not found.
    /// The list is edited in place, keyed by food, so a kept food keeps its row and never trips the unique index.
    /// </summary>
    public static async Task ApplyAsync(this Recipe recipe, IApplicationDbContext db, Guid userId, IRecipeFields fields,
        CancellationToken cancellationToken)
    {
        var ids = fields.Ingredients.Select(i => i.FoodId).ToList();
        var foods = await db.FoodItems.Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        var current = recipe.Ingredients.ToDictionary(i => i.FoodItemId);

        foreach (var id in ids)
        {
            if (!foods.TryGetValue(id, out var food) || !(food.IsVisibleTo(userId) || current.ContainsKey(id)))
            {
                throw new NotFoundException(FoodErrors.NotFound);
            }
        }

        recipe.Name = fields.Name.Trim();
        recipe.Description = Normalize(fields.Description);
        recipe.Instructions = Normalize(fields.Instructions);
        recipe.ServingsCount = fields.ServingsCount;
        recipe.PrepTimeMinutes = fields.PrepTimeMinutes;

        foreach (var removed in current.Values.Where(i => !ids.Contains(i.FoodItemId)))
        {
            recipe.Ingredients.Remove(removed);
        }

        foreach (var input in fields.Ingredients)
        {
            if (current.TryGetValue(input.FoodId, out var kept))
            {
                kept.QuantityGrams = input.QuantityGrams;
            }
            else
            {
                var ingredient = new RecipeIngredient
                {
                    Recipe = recipe, FoodItemId = input.FoodId, FoodItem = foods[input.FoodId], QuantityGrams = input.QuantityGrams
                };
                recipe.Ingredients.Add(ingredient);
                // Ids are generated client-side, so EF would take a new ingredient of a tracked recipe for an existing row.
                db.RecipeIngredients.Add(ingredient);
            }
        }
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
