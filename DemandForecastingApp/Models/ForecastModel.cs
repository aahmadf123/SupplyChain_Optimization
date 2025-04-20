using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Models
{
    public class ForecastModel
    {
        private readonly Random _random = new Random(42); // Use fixed seed for reproducibility
        
        // Forecast demand using SSA (Singular Spectrum Analysis)
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> PredictDemand(
            List<DemandRecord> historicalData, 
            int horizonDays = 10,
            float confidenceLevel = 0.95f)
        {
            if (historicalData == null || historicalData.Count == 0)
            {
                throw new ArgumentException("Historical data cannot be null or empty");
            }
            
            try
            {
                Logger.LogInfo($"Starting demand forecast with {historicalData.Count} historical records");
                
                // Sort data by date
                historicalData = historicalData.OrderBy(r => r.Date).ToList();
                
                // Get last date in historical data
                DateTime lastDate = historicalData.Last().Date;
                
                // Extract time series
                float[] timeSeries = historicalData.Select(r => r.Sales).ToArray();
                
                // Apply SSA forecasting
                float[] forecasts = ForecastWithSSA(timeSeries, horizonDays);
                
                // Calculate error margins based on historical volatility
                float stdDev = CalculateStdDev(timeSeries);
                
                // Calculate z-value for confidence interval
                float zValue = GetZValueForConfidenceLevel(confidenceLevel);
                
                // Create forecast results with dates and confidence intervals
                var result = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
                
                for (int i = 0; i < horizonDays; i++)
                {
                    DateTime forecastDate = lastDate.AddDays(i + 1);
                    float forecast = forecasts[i];
                    
                    // Error margin grows with forecast horizon
                    float errorMargin = zValue * stdDev * (float)Math.Sqrt(1 + i * 0.1);
                    
                    // Ensure lower bound is not negative
                    float lowerBound = Math.Max(0, forecast - errorMargin);
                    float upperBound = forecast + errorMargin;
                    
                    result.Add((forecastDate, forecast, lowerBound, upperBound));
                }
                
                Logger.LogInfo($"Forecast completed successfully for {horizonDays} days");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error forecasting demand", ex);
                throw;
            }
        }
        
        // SSA forecasting implementation
        private float[] ForecastWithSSA(float[] timeSeries, int horizonDays)
        {
            // Note: This is a simplified implementation of SSA forecasting.
            // In a real application, you would use a proper SSA library or implement
            // the full algorithm with embedding, decomposition, grouping, and reconstruction.
            
            int n = timeSeries.Length;
            float[] result = new float[horizonDays];
            
            // Window length (typically N/4 for monthly data)
            int windowLength = Math.Min(n / 4, 30);
            windowLength = Math.Max(windowLength, 2); // Ensure at least 2
            
            // Simple implementation using weighted moving average
            for (int i = 0; i < horizonDays; i++)
            {
                // Use the last 'windowLength' points for prediction
                float weightedSum = 0;
                float weightSum = 0;
                
                for (int j = 0; j < windowLength; j++)
                {
                    int idx = n - windowLength + j;
                    if (idx >= 0 && idx < n)
                    {
                        // More recent points have higher weights
                        float weight = windowLength - j;
                        weightedSum += timeSeries[idx] * weight;
                        weightSum += weight;
                    }
                }
                
                // Forecast for the next point
                float forecast = weightedSum / weightSum;
                
                // Add small trend component
                forecast += DetectTrend(timeSeries) * (i + 1);
                
                // Add small seasonal component (if any)
                forecast += DetectSeasonal(timeSeries, i);
                
                result[i] = Math.Max(0, forecast); // Ensure non-negative
                
                // Extend time series with this forecast for next iterations
                // (this creates a vector with original series + forecasts)
                float[] extended = new float[n + i + 1];
                Array.Copy(timeSeries, extended, n);
                Array.Copy(result, 0, extended, n, i + 1);
                
                // Use this extended series for next forecast
                timeSeries = extended;
                n = timeSeries.Length;
            }
            
            return result;
        }
        
        // Calculate standard deviation of time series
        private float CalculateStdDev(float[] timeSeries)
        {
            if (timeSeries == null || timeSeries.Length <= 1)
            {
                return 0;
            }
            
            float mean = timeSeries.Average();
            float sumOfSquares = timeSeries.Sum(x => (x - mean) * (x - mean));
            return (float)Math.Sqrt(sumOfSquares / (timeSeries.Length - 1));
        }
        
        // Detect trend in time series
        private float DetectTrend(float[] timeSeries)
        {
            if (timeSeries == null || timeSeries.Length <= 1)
            {
                return 0;
            }
            
            // Simple linear regression to detect trend
            int n = timeSeries.Length;
            float sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            
            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += timeSeries[i];
                sumXY += i * timeSeries[i];
                sumX2 += i * i;
            }
            
            // Calculate slope
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            
            return slope;
        }
        
        // Detect seasonal component
        private float DetectSeasonal(float[] timeSeries, int forecastIndex)
        {
            if (timeSeries == null || timeSeries.Length < 14) // Need at least 2 weeks
            {
                return 0;
            }
            
            // Weekly seasonality detection (simplified)
            int period = 7; // Days per week
            
            // Get day of week index for forecast
            int n = timeSeries.Length;
            int dayOfWeek = (n + forecastIndex) % period;
            
            // Calculate average value for this day of week
            float sum = 0;
            int count = 0;
            
            for (int i = dayOfWeek; i < n; i += period)
            {
                sum += timeSeries[i];
                count++;
            }
            
            // Calculate average deviation from overall average
            float dayAvg = count > 0 ? sum / count : 0;
            float overallAvg = timeSeries.Average();
            
            // Return seasonal component
            return dayAvg - overallAvg;
        }
        
        // Get Z-value for a given confidence level
        private float GetZValueForConfidenceLevel(float confidenceLevel)
        {
            // Simple approximation for commonly used confidence levels
            if (confidenceLevel >= 0.99f) return 2.58f;
            if (confidenceLevel >= 0.98f) return 2.33f;
            if (confidenceLevel >= 0.95f) return 1.96f;
            if (confidenceLevel >= 0.90f) return 1.65f;
            if (confidenceLevel >= 0.85f) return 1.44f;
            if (confidenceLevel >= 0.80f) return 1.28f;
            
            // Default
            return 1.96f;
        }
    }
}