using System;

namespace DemandForecastingApp.Models
{
    public class DemandRecord
    {
        public DateTime Date { get; set; }
        public float Sales { get; set; }
        public string StateHoliday { get; set; } = "0";
    }
}