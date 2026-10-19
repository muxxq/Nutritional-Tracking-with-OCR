using FluentAssertions;
using NutriScan.Domain.Common;
using Xunit;

namespace NutriScan.Domain.UnitTests.Common;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("Fără", "fara")]
    [InlineData("ȘNIȚEL", "snitel")]
    [InlineData("ş ţ Ş Ţ", "s t s t")]          // cedilla variants
    [InlineData("  Piept   de  PUI ", "piept de pui")]
    [InlineData("Îngheţată", "inghetata")]
    public void ForSearch_LowercasesAndStripsDiacritics(string input, string expected)
    {
        // Act
        var normalized = TextNormalizer.ForSearch(input);

        // Assert
        normalized.Should().Be(expected);
    }

    [Fact]
    public void SearchTerms_SplitsNormalizedWords()
    {
        // Act
        var terms = TextNormalizer.SearchTerms(" Piept  PUI ");

        // Assert
        terms.Should().Equal("piept", "pui");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void SearchTerms_BlankQuery_ReturnsNoTerms(string? query)
    {
        // Act
        var terms = TextNormalizer.SearchTerms(query);

        // Assert
        terms.Should().BeEmpty();
    }
}
