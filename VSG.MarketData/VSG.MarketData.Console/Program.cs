using System.Net.Http.Json;
using System.Text.Json;

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:8080";

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("VSG Market Data Client");
Console.WriteLine($"API: {apiBaseUrl}");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1. 24-hour average price");
    Console.WriteLine("2. Simple moving average");
    Console.WriteLine("0. Exit");
    Console.Write("Select option: ");

    var option = Console.ReadLine()?.Trim();

    if (option == "0")
        break;

    string path;

    if (option == "1")
    {
        var symbol = ReadRequired("Symbol (e.g. BTCUSDT): ").ToUpperInvariant();
        path = $"/api/{Uri.EscapeDataString(symbol)}/24hAvgPrice";
    }
    else if (option == "2")
    {
        var symbol = ReadRequired("Symbol (e.g. BTCUSDT): ").ToUpperInvariant();

        if (!int.TryParse(ReadRequired("Number of data points (n): "), out var n)
            || n <= 0)
        {
            Console.WriteLine("n must be a positive integer.");
            continue;
        }

        var period = ReadRequired("Period (1m, 5m, 30m, 1d, 1w): ");

        if (period is not ("1m" or "5m" or "30m" or "1d" or "1w"))
        {
            Console.WriteLine("Unsupported period.");
            continue;
        }

        Console.Write("Start/reference time in UTC (optional, e.g. 2026-10-20T11:41:00Z): ");
        var startTime = Console.ReadLine()?.Trim();

        path = $"/api/{Uri.EscapeDataString(symbol)}/SimpleMovingAverage" +
               $"?n={n}&p={Uri.EscapeDataString(period)}";

        if (!string.IsNullOrWhiteSpace(startTime))
        {
            if (!DateTimeOffset.TryParse(startTime, out var parsedTime))
            {
                Console.WriteLine("Invalid date/time.");
                continue;
            }

            path += $"&s={Uri.EscapeDataString(parsedTime.UtcDateTime.ToString("O"))}";
        }
    }
    else
    {
        Console.WriteLine("Unknown option.");
        continue;
    }

    try
    {
        using var response = await httpClient.GetAsync(path);
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(content);
            continue;
        }

        try
        {
            using var json = JsonDocument.Parse(content);
            Console.WriteLine(JsonSerializer.Serialize(
                json.RootElement,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (JsonException)
        {
            Console.WriteLine(content);
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Could not reach the API: {ex.Message}");
    }
}

static string ReadRequired(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var value = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        Console.WriteLine("A value is required.");
    }
}
