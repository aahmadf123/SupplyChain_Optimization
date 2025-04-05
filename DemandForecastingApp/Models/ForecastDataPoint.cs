using System;

namespace DemandForecastingApp.Models
{
    public class ForecastDataPoint
    {
        public string Period { get; set; }
        public float ForecastedDemand { get; set; }
        public float LowerBound { get; set; }
        public float UpperBound { get; set; }
        public float ReorderPoint { get; set; }
    }
}