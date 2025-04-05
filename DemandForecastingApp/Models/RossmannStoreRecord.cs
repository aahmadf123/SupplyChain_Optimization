namespace DemandForecastingApp.Models
{
    public class RossmannStoreRecord
    {
        public int StoreId { get; set; }
        public string StoreType { get; set; } = "Unknown";
        public string Assortment { get; set; } = "Unknown";
        public int? CompetitionDistance { get; set; }
        public int? CompetitionOpenSinceMonth { get; set; }
        public int? CompetitionOpenSinceYear { get; set; }
        public int Promo2 { get; set; }
        public int? Promo2SinceWeek { get; set; }
        public int? Promo2SinceYear { get; set; }
        public string PromoInterval { get; set; } = "None";
    }
}