using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NutriScan.Domain.Entities;

namespace NutriScan.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<FoodItem> FoodItems { get; }
    DbSet<DiaryEntry> DiaryEntries { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<PlannedMeal> PlannedMeals { get; }
    DbSet<ShoppingListItem> ShoppingListItems { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
