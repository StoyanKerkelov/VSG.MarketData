using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VSG.MarketData.Api.Contracts;
using VSG.MarketData.Api.Controllers;
using VSG.MarketData.Infrastructure.Persistence;

namespace VSG.MarketData.Tests;

public class PriceAnalysisControllerTests
{
    [Fact]
    public async Task GetSimpleMovingAverage_WhenFewerPointsAreAvailable_ReturnsOkWithWarning()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MarketDataDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new MarketDataDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var referenceTime = new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc);

        dbContext.PriceUpdates.AddRange(
            new Core.Models.PriceUpdate
            {
                Symbol = "BTCUSDT",
                Price = 100m,
                Timestamp = referenceTime.AddMinutes(-2)
            },
            new Core.Models.PriceUpdate
            {
                Symbol = "BTCUSDT",
                Price = 200m,
                Timestamp = referenceTime.AddMinutes(-1)
            });

        await dbContext.SaveChangesAsync();

        var controller = new PriceAnalysisController(dbContext);

        var result = await controller.GetSimpleMovingAverage("BTCUSDT", 5, "1m", referenceTime, CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, response.StatusCode);

        var body = Assert.IsType<SmaResponse>(response.Value);
        Assert.Equal(5, body.RequestedDataPoints);
        Assert.Equal(2, body.ActualDataPoints);
        Assert.NotNull(body.Warning);
        Assert.Contains("Requested 5 data points", body.Warning);
    }

    [Fact]
    public async Task GetSimpleMovingAverage_WhenAllPointsAreAvailable_ReturnsOkWithoutWarning()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MarketDataDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new MarketDataDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var referenceTime = new DateTime(2026, 10, 20, 12, 6, 0, DateTimeKind.Utc);

        dbContext.PriceUpdates.AddRange(
            new Core.Models.PriceUpdate
            {
                Symbol = "BTCUSDT",
                Price = 100m,
                Timestamp = new DateTime(2026, 10, 20, 12, 4, 10, DateTimeKind.Utc)
            },
            new Core.Models.PriceUpdate
            {
                Symbol = "BTCUSDT",
                Price = 200m,
                Timestamp = new DateTime(2026, 10, 20, 12, 5, 10, DateTimeKind.Utc)
            });

        await dbContext.SaveChangesAsync();

        var controller = new PriceAnalysisController(dbContext);

        var result = await controller.GetSimpleMovingAverage("BTCUSDT", 2, "1m", referenceTime, CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, response.StatusCode);

        var body = Assert.IsType<SmaResponse>(response.Value);
        Assert.Equal(2, body.RequestedDataPoints);
        Assert.Equal(2, body.ActualDataPoints);
        Assert.Null(body.Warning);
    }

    [Fact]
    public async Task GetSimpleMovingAverage_WhenNoDataExists_ReturnsNotFound()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MarketDataDbContext>().UseSqlite(connection).Options;

        await using var dbContext = new MarketDataDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var controller = new PriceAnalysisController(dbContext);

        var result = await controller.GetSimpleMovingAverage(
            "BTCUSDT",
            5,
            "1m",
            new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc),
            CancellationToken.None);

        var response = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, response.StatusCode);
    }
}
