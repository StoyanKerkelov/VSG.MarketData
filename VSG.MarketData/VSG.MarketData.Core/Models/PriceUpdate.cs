namespace VSG.MarketData.Core.Models;

public class PriceUpdate
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}