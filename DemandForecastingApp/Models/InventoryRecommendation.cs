namespace DemandForecastingApp.Models
{
    public class InventoryRecommendation
    {
        public string Item { get; set; }
        public int CurrentStock { get; set; }
        public int RecommendedOrder { get; set; }
        public double LeadTimeDemand { get; set; }
        public double SafetyStock { get; set; }
    }
}