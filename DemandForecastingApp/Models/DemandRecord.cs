using System;

namespace DemandForecastingApp.Models
{
    public class DemandRecord
    {
        public DateTime Date { get; set; }
        public float Demand { get; set; }
        public string? ProductId { get; set; }
    }
}