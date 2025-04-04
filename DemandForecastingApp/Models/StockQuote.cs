namespace DemandForecastingApp.Models
{
    public class StockQuote
    {
        public required string Symbol { get; set; }
        public required string Price { get; set; }
        public required string Change { get; set; }
    }
}