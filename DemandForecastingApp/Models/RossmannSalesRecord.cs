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
        public string StateHoliday { get; set; } = "0";
        public int SchoolHoliday { get; set; }
        public int Promo { get; set; }
        
        // Fields from store data
        public string StoreType { get; set; } = "Unknown";
        public string Assortment { get; set; } = "Unknown";
        public int? CompetitionDistance { get; set; }
        public int? CompetitionOpenSinceMonth { get; set; }
        public int? CompetitionOpenSinceYear { get; set; }
        public int Promo2 { get; set; }
        public int? Promo2SinceWeek { get; set; }
        public int? Promo2SinceYear { get; set; }
        public string PromoInterval { get; set; } = "None";
        
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
            return new float[]
            {
                StoreId,
                Year,
                Month,
                Day,
                DayOfWeek,
                IsWeekend ? 1 : 0,
                Open,
                Promo,
                IsPublicHoliday ? 1 : 0,
                SchoolHoliday,
                CompetitionDistance ?? -1,
                CompetitionOpenMonths,
                Promo2,
                Promo2ActiveMonths,
                Promo2Active ? 1 : 0
            };
        }
        
        // Label for ML (what we're trying to predict)
        public float GetLabel()
        {
            return Sales ?? 0;
        }
    }
}