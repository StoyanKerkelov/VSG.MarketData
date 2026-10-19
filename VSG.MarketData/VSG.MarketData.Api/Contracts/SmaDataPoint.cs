
namespace VSG.MarketData.Api.Contracts;

public class SmaDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
}

public class SmaResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int RequestedDataPoints { get; set; }
    public int ActualDataPoints { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public decimal SimpleMovingAverage { get; set; }
    public List<SmaDataPoint> DataPoints { get; set; } = [];
}
