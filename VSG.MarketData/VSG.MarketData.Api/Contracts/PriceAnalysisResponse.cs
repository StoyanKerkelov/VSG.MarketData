
namespace VSG.MarketData.Api.Contracts;

public class PriceAnalysisResponse
{
    public string Symbol { get; set; } = string.Empty;
    public decimal AveragePrice { get; set; }
    public int DataPoints { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
}
