namespace NutriScan.Application.ShoppingList;

public static class ShoppingListErrors
{
    public const string NotFound = "Articolul nu a fost găsit în lista de cumpărături.";
    public const string NothingPlanned = "Nu există mese planificate nemâncate în intervalul ales. Planifică mai întâi mesele.";
    public const string ConcurrentChange = "Lista de cumpărături a fost modificată în același timp de altă cerere. Reîncearcă.";
}
