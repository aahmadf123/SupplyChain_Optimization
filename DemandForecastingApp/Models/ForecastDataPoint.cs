using System;

namespace DemandForecastingApp.Models
{
    public class ForecastDataPoint
    {
        public string Period { get; set; }
        public DateTime Date { get; set; }
        public double ForecastedDemand { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public string ReorderPoint { get; set; }
    }
}