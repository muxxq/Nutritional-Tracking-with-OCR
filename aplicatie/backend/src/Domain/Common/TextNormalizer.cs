using System.Globalization;
using System.Text;

namespace NutriScan.Domain.Common;

/// <summary>
/// Search-friendly text: lowercase, no diacritics, single spaces. "Piept de pui FĂRĂ piele" → "piept de pui fara piele".
/// Diacritics are removed by Unicode decomposition (NFD), which covers both comma (ș, ț) and cedilla (ş, ţ) forms.
/// </summary>
public static class TextNormalizer
{
    public static string ForSearch(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsWhiteSpace(c) ? ' ' : c);
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Normalize(NormalizationForm.FormC);
    }

    public static IReadOnlyList<string> SearchTerms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? Array.Empty<string>()
            : ForSearch(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
