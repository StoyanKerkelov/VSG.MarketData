
namespace VSG.MarketData.Core.Services;

public static class SimpleMovingAverageCalculator
{
    public static decimal Calculate(IEnumerable<decimal> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);

        var values = prices.ToList();

        if (values.Count == 0)
            throw new ArgumentException("At least one price is required.", nameof(prices));

        return values.Average();
    }
}
