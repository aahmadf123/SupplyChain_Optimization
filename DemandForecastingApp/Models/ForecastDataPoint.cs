using System;

namespace DemandForecastingApp.Models
{
    public class ForecastDataPoint
    {
        public required string Period { get; set; }
        public DateTime Date { get; set; }
        public double ForecastedDemand { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public required string ReorderPoint { get; set; }
    }
}