
using VSG.MarketData.Core.Services;

namespace VSG.MarketData.Tests;

public class SimpleMovingAverageCalculatorTests
{
    [Fact]
    public void Calculate_WithMultiplePrices_ReturnsAverage()
    {
        var prices = new decimal[] { 10m, 20m, 30m };

        var result = SimpleMovingAverageCalculator.Calculate(prices);

        Assert.Equal(20m, result);
    }

    [Fact]
    public void Calculate_WithSinglePrice_ReturnsThatPrice()
    {
        var result = SimpleMovingAverageCalculator.Calculate(new[] { 42.5m });

        Assert.Equal(42.5m, result);
    }

    [Fact]
    public void Calculate_WithNoPrices_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => SimpleMovingAverageCalculator.Calculate(Array.Empty<decimal>()));
    }
}
