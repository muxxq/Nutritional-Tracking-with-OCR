namespace NutriScan.Domain.Nutrition;

/// <summary>
/// FR-06: declared energy should match 4·P + 4·C + 9·G within 15%. A mismatch is a warning, not an error:
/// fibre (~2 kcal/g), alcohol (7 kcal/g) and polyols legitimately shift the total. Also used to check OCR results.
/// </summary>
public static class EnergyCheck
{
    public const double Tolerance = 0.15;

    public static double CalculatedKcal(double proteins, double carbs, double fats) => 4 * proteins + 4 * carbs + 9 * fats;

    public static string? Warning(double declaredKcal, double proteins, double carbs, double fats)
    {
        var calculated = CalculatedKcal(proteins, carbs, fats);

        if (declaredKcal == 0)
        {
            return calculated == 0
                ? null
                : $"Caloriile declarate (0 kcal) nu corespund macronutrienților ({Round(calculated)} kcal). Verifică valorile.";
        }

        var deviation = Math.Abs(calculated - declaredKcal) / declaredKcal;
        return deviation <= Tolerance
            ? null
            : $"Caloriile declarate ({Round(declaredKcal)} kcal) diferă cu {Round(deviation * 100)}% de cele calculate " +
              $"din macronutrienți ({Round(calculated)} kcal). Verifică valorile.";
    }

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
