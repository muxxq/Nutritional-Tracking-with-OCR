using FluentAssertions;
using NutriScan.Domain.Nutrition;
using Xunit;

namespace NutriScan.Domain.UnitTests.Nutrition;

public class PlanDayStatusTests
{
    [Theory]
    [InlineData(1614.9, PlanDayStatus.Under)]
    [InlineData(1615, PlanDayStatus.Within)]
    [InlineData(1785, PlanDayStatus.Within)]
    [InlineData(1785.1, PlanDayStatus.Over)]
    [InlineData(0, PlanDayStatus.Under)]
    public void Of_Target1700_ClassifiesBoundaries(double totalKcal, PlanDayStatus expected)
    {
        // Act
        var status = PlanDayStatusCalculator.Of(totalKcal, 1700);

        // Assert
        status.Should().Be(expected);
    }
}
