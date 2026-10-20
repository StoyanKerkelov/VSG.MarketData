
using VSG.MarketData.Core.Models;
using VSG.MarketData.Core.Services;

namespace VSG.MarketData.Tests;

public class PriceIntervalCalculatorTests
{
    [Fact]
    public void Calculate_GroupsPricesByMinute_AndAveragesEachGroup()
    {
        var prices = new[]
        {
            CreatePrice(10m, "2026-10-20T11:36:10Z"),
            CreatePrice(20m, "2026-10-20T11:36:30Z"),
            CreatePrice(30m, "2026-10-20T11:37:10Z")
        };

        var result = PriceIntervalCalculator.Calculate(
            prices, TimeSpan.FromMinutes(1), 5);

        Assert.Equal(2, result.Count);
        Assert.Equal(15m, result[0].Price);
        Assert.Equal(30m, result[1].Price);
    }

    [Fact]
    public void Calculate_ReturnsLatestNIntervals_InChronologicalOrder()
    {
        var prices = new[]
        {
            CreatePrice(10m, "2026-10-20T11:36:10Z"),
            CreatePrice(20m, "2026-10-20T11:37:10Z"),
            CreatePrice(30m, "2026-10-20T11:38:10Z")
        };

        var result = PriceIntervalCalculator.Calculate(
            prices, TimeSpan.FromMinutes(1), 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(20m, result[0].Price);
        Assert.Equal(30m, result[1].Price);
        Assert.True(result[0].Timestamp < result[1].Timestamp);
    }

    private static PriceUpdate CreatePrice(decimal price, string timestamp) =>
        new()
        {
            Symbol = "BTCUSDT",
            Price = price,
            Timestamp = DateTime.Parse(timestamp).ToUniversalTime()
        };
}
