using VSG.MarketData.Core.Models;

namespace VSG.MarketData.Core.Services;

public static class PriceIntervalCalculator
{
    public static IReadOnlyList<(DateTime Timestamp, decimal Price)> Calculate(IEnumerable<PriceUpdate> prices, TimeSpan period, int n)
    {
        ArgumentNullException.ThrowIfNull(prices);

        if (period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period));

        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        return prices.GroupBy(x => new DateTime(
                         x.Timestamp.Ticks / period.Ticks * period.Ticks,
                         DateTimeKind.Utc))
                     .Select(g => (
                         Timestamp: g.Key,
                         Price: g.Average(x => x.Price)))
                     .OrderByDescending(x => x.Timestamp)
                     .Take(n)
                     .OrderBy(x => x.Timestamp)
                     .ToList();
    }
}
