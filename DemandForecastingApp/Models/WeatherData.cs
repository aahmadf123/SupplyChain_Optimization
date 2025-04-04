namespace DemandForecastingApp.Models
{
    public class WeatherData
    {
        public required string Location { get; set; }
        public float Temperature { get; set; }
        public required string Condition { get; set; }
        public float Precipitation { get; set; }
    }
}