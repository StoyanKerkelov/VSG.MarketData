namespace VSG.MarketData.Core.Models;

public class MonitoredSymbol
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}