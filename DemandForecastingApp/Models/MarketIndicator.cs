namespace DemandForecastingApp.Models
{
    public class MarketIndicator
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string? Change { get; set; }
        public string? Impact { get; set; }

        // Default parameterless constructor for serialization/deserialization
        public MarketIndicator() { }

        // Constructor that initializes all properties
        public MarketIndicator(string key, string value, string? change = null, string? impact = null)
        {
            Key = key;
            Value = value;
            Change = change;
            Impact = impact;
        }
    }
}