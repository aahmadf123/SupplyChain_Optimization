namespace DemandForecastingApp.Models
{
    public class StockQuote
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public string Change { get; set; }
    }
}