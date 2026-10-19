namespace NutriScan.Application.Diary;

public static class DiaryErrors
{
    public const string NotFound = "Intrarea din jurnal nu a fost găsită.";
    public const string ExactlyOneItem = "Alege fie un aliment, fie o rețetă.";
    public const string RecipeServingsOnly = "Rețetele se adaugă în porții, nu în grame.";
}
