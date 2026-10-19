using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VSG.MarketData.Api.Contracts;
using VSG.MarketData.Api.Extensions;
using VSG.MarketData.Infrastructure.Persistence;

namespace VSG.MarketData.Api.Controllers;

[ApiController]
[Route("api/{symbol}")]
public class PriceAnalysisController(MarketDataDbContext dbContext)
    : ControllerBase
{
    [HttpGet("24hAvgPrice")]
    public async Task<IActionResult> Get24HourAverage(string symbol, CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();

        var since = DateTime.UtcNow.AddHours(-24);

        var prices = await dbContext.PriceUpdates
            .AsNoTracking()
            .Where(x => x.Symbol == symbol && x.Timestamp >= since)
            .Select(x => x.Price)
            .ToListAsync(cancellationToken);

        if (prices.Count == 0)
            return NotFound($"No price data available for {symbol}.");

        var average = prices.Average();

        return this.Response(StatusCodes.Status200OK,
            new PriceAnalysisResponse
            {
                Symbol = symbol,
                AveragePrice = average,
                DataPoints = prices.Count,
                FromUtc = since,
                ToUtc = DateTime.UtcNow
            });
    }


    [HttpGet("SimpleMovingAverage")]
    public async Task<IActionResult> GetSimpleMovingAverage(string symbol, [FromQuery] int n, [FromQuery] string p, [FromQuery] DateTime? s,
        CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();

        if (n <= 0)
            return BadRequest("Parameter n must be greater than zero.");

        if (string.IsNullOrWhiteSpace(symbol))
            return this.Response(StatusCodes.Status400BadRequest, "Symbol is required.");

        if (n <= 0 || n > 1000)
            return this.Response(StatusCodes.Status400BadRequest, "Parameter n must be between 1 and 1000.");

        var period = p switch
        {
            "1m" => TimeSpan.FromMinutes(1),
            "5m" => TimeSpan.FromMinutes(5),
            "30m" => TimeSpan.FromMinutes(30),
            "1d" => TimeSpan.FromDays(1),
            "1w" => TimeSpan.FromDays(7),
            _ => (TimeSpan?)null
        };

        if (period is null)
            return BadRequest("Supported periods: 1m, 5m, 30m, 1d, 1w.");

        var referenceTime = s.HasValue ? DateTime.SpecifyKind(s.Value, DateTimeKind.Utc)
                                       : DateTime.UtcNow;

        var startTime = referenceTime - TimeSpan.FromTicks(period.Value.Ticks * (long)n);

        var prices = await dbContext.PriceUpdates.AsNoTracking()
                                                 .Where(x => x.Symbol == symbol
                                                             && x.Timestamp > startTime
                                                             && x.Timestamp <= referenceTime)
                                                 .OrderBy(x => x.Timestamp)
                                                 .Select(x => new { x.Price, x.Timestamp })
                                                 .ToListAsync(cancellationToken);

        if (prices.Count == 0)
            return NotFound($"No price data available for {symbol} in the requested period.");

        var points = prices.GroupBy(x => new DateTime(
                                    x.Timestamp.Ticks / period.Value.Ticks * period.Value.Ticks,
                                    DateTimeKind.Utc))
                           .Select(g => new
                           {
                               Timestamp = g.Key,
                               Price = g.Average(x => x.Price)
                           })
                           .OrderByDescending(x => x.Timestamp)
                           .Take(n)
                           .OrderBy(x => x.Timestamp)
                           .ToList();

        if (points.Count == 0)
            return NotFound("No data points available.");

        if (points.Count < n)
            return UnprocessableEntity($"Insufficient data for SMA: requested {n} points, but only {points.Count} are available.");

        return this.Response(StatusCodes.Status200OK, new SmaResponse
        {
            Symbol = symbol,
            Period = p,
            RequestedDataPoints = n,
            ActualDataPoints = points.Count,
            StartTimeUtc = points.First().Timestamp,
            EndTimeUtc = points.Last().Timestamp,
            SimpleMovingAverage = points.Average(x => x.Price),
            DataPoints = points.Select(x => new SmaDataPoint
            {
                Timestamp = x.Timestamp,
                Price = x.Price
            }).ToList()
        });
    }
}
