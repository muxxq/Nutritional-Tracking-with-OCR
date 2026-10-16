namespace NutriScan.Application.Planner;

public static class PlannerErrors
{
    public const string NotFound = "Masa planificată nu a fost găsită.";
    public const string ExactlyOneItem = "Alege fie un aliment, fie o rețetă.";
    public const string RecipeServingsOnly = "Rețetele se planifică în porții, nu în grame.";
    public const string AlreadyEaten = "Masa planificată a fost deja trecută în jurnal.";
    public const string FutureMeal = "O masă planificată după ziua de mâine nu poate fi trecută în jurnal.";
    public const string DateRange = "Data trebuie să fie între 01.01.2000 și un an de acum înainte.";
}
