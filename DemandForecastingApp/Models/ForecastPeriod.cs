using System;

namespace DemandForecastingApp.Models
{
    /// <summary>
    /// Represents a single period in a forecast
    /// </summary>
    public class ForecastPeriod
    {
        /// <summary>
        /// The date for this forecast period
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The forecasted value
        /// </summary>
        public float Forecast { get; set; }

        /// <summary>
        /// The lower bound of the confidence interval
        /// </summary>
        public float LowerBound { get; set; }

        /// <summary>
        /// The upper bound of the confidence interval
        /// </summary>
        public float UpperBound { get; set; }

        /// <summary>
        /// The actual value (if available)
        /// </summary>
        public float? ActualValue { get; set; }

        /// <summary>
        /// The forecast error (if actual value is available)
        /// </summary>
        public float? Error => ActualValue.HasValue ? Math.Abs((Forecast - ActualValue.Value) / ActualValue.Value) : null;

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd}: Forecast={Forecast:F2}, Bounds=[{LowerBound:F2}, {UpperBound:F2}]" +
                   (ActualValue.HasValue ? $", Actual={ActualValue:F2}, Error={Error:P2}" : "");
        }
    }
} 