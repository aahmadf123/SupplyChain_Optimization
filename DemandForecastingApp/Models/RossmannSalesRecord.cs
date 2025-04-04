using System;

namespace DemandForecastingApp.Models
{
    public class RossmannSalesRecord
    {
        // Original fields from sales data
        public int StoreId { get; set; }
        public DateTime Date { get; set; }
        public float? Sales { get; set; }
        public int? Customers { get; set; }
        public int Open { get; set; }
        public required string StateHoliday { get; set; }
        public int SchoolHoliday { get; set; }
        public int Promo { get; set; }
        
        // Fields from store data
        public required string StoreType { get; set; }
        public required string Assortment { get; set; }
        public int? CompetitionDistance { get; set; }
        public int? CompetitionOpenSinceMonth { get; set; }
        public int? CompetitionOpenSinceYear { get; set; }
        public int Promo2 { get; set; }
        public int? Promo2SinceWeek { get; set; }
        public int? Promo2SinceYear { get; set; }
        public required string PromoInterval { get; set; }
        
        // Engineered features
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int DayOfWeek { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsPublicHoliday { get; set; }
        public bool IsEasterHoliday { get; set; }
        public bool IsChristmas { get; set; }
        public int CompetitionOpenMonths { get; set; }
        public int Promo2ActiveMonths { get; set; }
        public bool Promo2Active { get; set; }
        
        // Convert to vector for ML
        public float[] ToFeatureVector()
        {
            // Example feature vector - adjust based on your needs
            return new float[]
            {
                DayOfWeek,
                Month,
                IsWeekend ? 1 : 0,
                IsPublicHoliday ? 1 : 0,
                IsEasterHoliday ? 1 : 0,
                IsChristmas ? 1 : 0,
                Promo,
                SchoolHoliday,
                Promo2 == 1 ? 1 : 0,
                CompetitionOpenMonths > 0 ? CompetitionOpenMonths : 0
            };
        }
        
        // Label for ML (what we're trying to predict)
        public float GetLabel()
        {
            return Sales ?? 0f;
        }
    }
}