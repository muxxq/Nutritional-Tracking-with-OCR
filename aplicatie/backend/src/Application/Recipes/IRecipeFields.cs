namespace NutriScan.Application.Recipes;

public sealed record RecipeIngredientInput(Guid FoodId, double QuantityGrams);

/// <summary>Fields shared by creating and editing a recipe; the ingredient list is replaced as a whole (ADR-006 D5).</summary>
public interface IRecipeFields
{
    string Name { get; }
    string? Description { get; }
    string? Instructions { get; }
    int ServingsCount { get; }
    int? PrepTimeMinutes { get; }
    IReadOnlyList<RecipeIngredientInput> Ingredients { get; }
}
