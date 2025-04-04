using System;

namespace DemandForecastingApp.Models
{
    public class DemandRecord
    {
        public int StoreId { get; set; }
        public DateTime Date { get; set; }
        public double Sales { get; set; }
        public int Customers { get; set; }
        public bool Open { get; set; }
        public bool Promo { get; set; }
        public required string StateHoliday { get; set; }
        public bool SchoolHoliday { get; set; }
    }
}