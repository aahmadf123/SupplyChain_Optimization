using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    public class ForecastModel
    {
        private readonly Random _random = new Random(42);
        
        // Forecasting parameters
        private int _windowSize = 7; // For moving average and other window-based methods
        private double _alpha = 0.2; // For exponential smoothing
        
        public ForecastModel()
        {
            // Load parameters from settings if available
            try
            {
                string windowSizeStr = AppSettings.GetSetting("WindowSize", "7");
                string alphaStr = AppSettings.GetSetting("SmoothingAlpha", "0.2");
                
                if (int.TryParse(windowSizeStr, out int windowSize) && windowSize > 0)
                {
                    _windowSize = windowSize;
                }
                
                if (double.TryParse(alphaStr, out double alpha) && alpha > 0 && alpha < 1)
                {
                    _alpha = alpha;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error loading forecast parameters: {ex.Message}. Using defaults.");
            }
        }
        
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> PredictDemand(
            List<DemandRecord> historicalData, 
            int horizon)
        {
            try
            {
                Logger.LogInfo($"Forecasting demand for {horizon} days using SSA");
                
                if (historicalData == null || historicalData.Count == 0)
                {
                    throw new ArgumentException("Historical data cannot be empty");
                }
                
                // Prepare time series data
                var timestamps = historicalData.Select(r => r.Date).ToList();
                var values = historicalData.Select(r => r.Sales).ToList();
                
                // Sort by date if needed
                if (timestamps.Zip(timestamps.Skip(1), (a, b) => a > b).Any(x => x))
                {
                    var combined = timestamps.Zip(values, (t, v) => new { Date = t, Value = v })
                        .OrderBy(x => x.Date)
                        .ToList();
                    
                    timestamps = combined.Select(x => x.Date).ToList();
                    values = combined.Select(x => x.Value).ToList();
                }

                // Get the date range for the forecast period
                DateTime lastDate = timestamps.Last();
                var forecastDates = Enumerable.Range(1, horizon)
                    .Select(i => lastDate.AddDays(i))
                    .ToList();
                
                // Compute seasonality (simplistic approach for demonstration)
                var seasonality = ComputeSeasonality(values, 7); // Weekly seasonality
                
                // Decompose time series (simplified SSA-like approach)
                var (trend, seasonal, residuals) = DecomposeTimeSeries(values, 7);
                
                // Create forecast using components
                var forecasts = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
                
                // Calculate error statistics for confidence intervals
                double residualStdev = CalculateStandardDeviation(residuals);
                double confidenceFactor = 1.96; // 95% confidence interval (normal distribution)
                
                for (int i = 0; i < horizon; i++)
                {
                    var forecastDate = forecastDates[i];
                    
                    // Project trend (simple linear extrapolation)
                    double trendComponent = trend.Last() + 
                        (i + 1) * (trend.Last() - trend[trend.Count - 2]);
                    
                    // Apply seasonality
                    int seasonIndex = i % seasonality.Count;
                    double seasonalComponent = seasonality[seasonIndex];
                    
                    // Add some randomness based on residual statistics
                    double randomComponent = 0; // Could add random noise here based on residual distribution
                    
                    // Combine components for final forecast
                    float forecast = (float)Math.Max(0, trendComponent + seasonalComponent + randomComponent);
                    
                    // Calculate confidence intervals
                    float errorMargin = (float)(confidenceFactor * residualStdev * Math.Sqrt(i + 1));
                    float lowerBound = Math.Max(0, forecast - errorMargin);
                    float upperBound = forecast + errorMargin;
                    
                    forecasts.Add((forecastDate, forecast, lowerBound, upperBound));
                }
                
                return forecasts;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in demand forecasting", ex);
                throw;
            }
        }
        
        // Simplified SSA-like decomposition
        private (List<double> Trend, List<double> Seasonal, List<double> Residuals) DecomposeTimeSeries(
            List<float> values,
            int seasonalPeriod)
        {
            var trend = ComputeTrend(values);
            var detrended = values.Zip(trend, (v, t) => (double)v - t).ToList();
            var seasonal = ComputeSeasonality(values, seasonalPeriod);
            
            // Calculate residuals by removing both trend and seasonality
            var residuals = new List<double>();
            for (int i = 0; i < values.Count; i++)
            {
                int seasonIndex = i % seasonal.Count;
                double expected = trend[i] + seasonal[seasonIndex];
                residuals.Add(values[i] - expected);
            }
            
            return (trend, seasonal, residuals);
        }
        
        // Compute trend using moving average
        private List<double> ComputeTrend(List<float> values)
        {
            var trend = new List<double>();
            
            for (int i = 0; i < values.Count; i++)
            {
                // Calculate centered moving average
                int halfWindow = _windowSize / 2;
                int start = Math.Max(0, i - halfWindow);
                int end = Math.Min(values.Count - 1, i + halfWindow);
                int count = end - start + 1;
                
                double sum = 0;
                for (int j = start; j <= end; j++)
                {
                    sum += values[j];
                }
                
                trend.Add(sum / count);
            }
            
            return trend;
        }
        
        // Extract seasonal pattern
        private List<double> ComputeSeasonality(List<float> values, int period)
        {
            var seasonalPattern = new List<double>(new double[period]);
            int[] counts = new int[period];
            
            // Compute average values by position in period
            for (int i = 0; i < values.Count; i++)
            {
                int posInPeriod = i % period;
                seasonalPattern[posInPeriod] += values[i];
                counts[posInPeriod]++;
            }
            
            // Normalize by counts
            for (int i = 0; i < period; i++)
            {
                if (counts[i] > 0)
                {
                    seasonalPattern[i] /= counts[i];
                }
            }
            
            // Center the seasonal component around zero
            double average = seasonalPattern.Average();
            return seasonalPattern.Select(x => x - average).ToList();
        }
        
        // Calculate standard deviation for confidence intervals
        private double CalculateStandardDeviation(List<double> values)
        {
            double mean = values.Average();
            double sumSquaredDiff = values.Sum(x => Math.Pow(x - mean, 2));
            return Math.Sqrt(sumSquaredDiff / (values.Count - 1));
        }
    }
}