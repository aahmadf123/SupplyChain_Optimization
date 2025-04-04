namespace DemandForecastingApp.Models
{
    public class StoreRecord
    {
        public int StoreId { get; set; }
        public required string StoreType { get; set; }
        public required string Assortment { get; set; }
        public double? CompetitionDistance { get; set; }
        public int? CompetitionOpenSinceMonth { get; set; }
        public int? CompetitionOpenSinceYear { get; set; }
        public bool Promo2 { get; set; }
        public int? Promo2SinceWeek { get; set; }
        public int? Promo2SinceYear { get; set; }
        public required string PromoInterval { get; set; }
    }
}