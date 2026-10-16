using NutriScan.Domain.Entities;

namespace NutriScan.Domain.Nutrition;

/// <summary>Energy (kcal) and macronutrients (g), unrounded; rounding is a display concern.</summary>
public sealed record Macros(double Calories, double Proteins, double Carbs, double Fats)
{
    public static readonly Macros Zero = new(0, 0, 0, 0);

    /// <summary>What <paramref name="grams"/> of a food contribute; the food's values are per 100 g.</summary>
    public static Macros Of(FoodItem food, double grams) =>
        new Macros(food.Calories, food.Proteins, food.Carbohydrates, food.Fats) * (grams / 100.0);

    public static Macros operator +(Macros a, Macros b) =>
        new(a.Calories + b.Calories, a.Proteins + b.Proteins, a.Carbs + b.Carbs, a.Fats + b.Fats);

    public static Macros operator *(Macros m, double factor) =>
        new(m.Calories * factor, m.Proteins * factor, m.Carbs * factor, m.Fats * factor);

    public static Macros operator /(Macros m, double divisor) =>
        new(m.Calories / divisor, m.Proteins / divisor, m.Carbs / divisor, m.Fats / divisor);
}
