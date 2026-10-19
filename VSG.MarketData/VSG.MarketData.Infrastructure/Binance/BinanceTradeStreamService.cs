
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VSG.MarketData.Core.Models;
using VSG.MarketData.Infrastructure.Persistence;

namespace VSG.MarketData.Infrastructure.Binance;

public sealed class BinanceTradeStreamService(
    IServiceScopeFactory scopeFactory,
    ILogger<BinanceTradeStreamService> logger)
    : BackgroundService
{
    private static readonly Uri BaseUri =
        new("wss://stream.binance.com:9443/stream");

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var symbols = await GetActiveSymbolsAsync(stoppingToken);

                if (symbols.Length == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var streams = string.Join("/",
                    symbols.Select(s => $"{s.ToLowerInvariant()}@trade"));

                var uri = new Uri(
                    $"{BaseUri}?streams={Uri.EscapeDataString(streams).Replace("%2F", "/")}"
                );

                logger.LogInformation("Connecting to Binance: {Uri}", uri);

                using var socket = new ClientWebSocket();
                await socket.ConnectAsync(uri, stoppingToken);

                logger.LogInformation(
                    "Connected to Binance for symbols: {Symbols}",
                    string.Join(", ", symbols));

                logger.LogInformation(
                    "Connected to Binance for symbols: {Symbols}",
                    string.Join(", ", symbols));

                await ReceiveMessagesAsync(socket, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Binance stream failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<string[]> GetActiveSymbolsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketDataDbContext>();

        return await db.MonitoredSymbols
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Symbol)
            .ToArrayAsync(cancellationToken);
    }

    private async Task ReceiveMessagesAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];

        while (socket.State == WebSocketState.Open &&
               !cancellationToken.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            await ProcessMessageAsync(
                message.ToArray(), cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        byte[] payload,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var data = root.GetProperty("data");

            var symbol = data.GetProperty("s").GetString();
            var priceText = data.GetProperty("p").GetString();
            var timestampMs = data.GetProperty("T").GetInt64();

            if (string.IsNullOrWhiteSpace(symbol) ||
                !decimal.TryParse(
                    priceText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var price))
            {
                logger.LogWarning("Invalid Binance trade message.");
                return;
            }

            var update = new PriceUpdate
            {
                Symbol = symbol,
                Price = price,
                Timestamp = DateTimeOffset
                    .FromUnixTimeMilliseconds(timestampMs)
                    .UtcDateTime
            };

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider
                .GetRequiredService<MarketDataDbContext>();

            logger.LogInformation("Received Binance trade: {Symbol} at {Price}, time {Timestamp}",
                                    update.Symbol, update.Price, update.Timestamp);

            db.PriceUpdates.Add(update);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not parse Binance message.");
        }
    }
}
