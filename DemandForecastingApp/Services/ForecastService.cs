using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemandForecastingApp.ML;
using DemandForecastingApp.Models;
using DemandForecastingApp.Data;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Services
{
    /// <summary>
    /// Service for handling forecasting operations and predictions
    /// </summary>
    public class ForecastService
    {
        private readonly ForecasterFactory _forecasterFactory;
        private readonly Dictionary<string, IForecaster> _trainedModels;
        
        public ForecastService()
        {
            _forecasterFactory = new ForecasterFactory();
            _trainedModels = new Dictionary<string, IForecaster>();
        }
        
        /// <summary>
        /// Runs a forecast using the specified model type and data
        /// </summary>
        /// <param name="salesData">Historical sales data to use for forecasting</param>
        /// <param name="modelType">Type of forecasting model to use (e.g., "SSA", "LSTM")</param>
        /// <param name="horizon">Number of periods to forecast</param>
        /// <returns>Forecast results with confidence intervals</returns>
        public async Task<List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>> RunForecastAsync(
            List<RossmannSalesRecord> salesData,
            string modelType,
            int horizon)
        {
            try
            {
                Logger.LogInfo($"Running forecast with model type '{modelType}' for {horizon} periods");
                
                // Validate inputs
                if (salesData == null || salesData.Count == 0)
                {
                    throw new ArgumentException("Sales data cannot be empty");
                }
                
                if (horizon <= 0)
                {
                    throw new ArgumentException("Forecast horizon must be positive");
                }
                
                // Check if we already have a trained model of this type
                if (!_trainedModels.TryGetValue(modelType, out var forecaster))
                {
                    // Create a new forecaster
                    forecaster = _forecasterFactory.CreateForecaster(modelType);
                    
                    // Store for future use
                    _trainedModels[modelType] = forecaster;
                }
                
                // Split data into training and recent sets
                int trainingSize = (int)(salesData.Count * 0.8); // Use 80% of data for training
                var trainingData = salesData.Take(trainingSize).ToList();
                var recentData = salesData.Skip(trainingSize).ToList();
                
                if (recentData.Count == 0)
                {
                    recentData = trainingData.TakeLast(Math.Min(30, trainingData.Count)).ToList();
                }
                
                // Train the model
                await Task.Run(() => forecaster.Train(trainingData));
                
                // Make predictions
                var forecastResults = await Task.Run(() => forecaster.Predict(recentData, horizon));
                
                Logger.LogInfo($"Successfully completed forecast with {forecastResults.Count} results");
                
                return forecastResults;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                throw;
            }
        }
        
        /// <summary>
        /// Generates inventory recommendations based on forecast results
        /// </summary>
        /// <param name="forecast">Forecast results</param>
        /// <param name="leadTime">Lead time for ordering (in days)</param>
        /// <param name="serviceLevel">Service level (e.g., 0.95 for 95% service level)</param>
        /// <param name="currentStock">Current inventory levels</param>
        /// <returns>List of inventory recommendations</returns>
        public List<InventoryRecommendation> GenerateRecommendations(
            List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecast,
            int leadTime,
            double serviceLevel,
            Dictionary<string, int> currentStock)
        {
            try
            {
                Logger.LogInfo($"Generating inventory recommendations with lead time {leadTime}, service level {serviceLevel}");
                
                var recommendations = new List<InventoryRecommendation>();
                
                // Calculate safety factor based on service level
                // For normal distribution: 90% = 1.28, 95% = 1.65, 99% = 2.33
                double safetyFactor = 1.65; // Default to 95%
                if (serviceLevel >= 0.99)
                {
                    safetyFactor = 2.33;
                }
                else if (serviceLevel >= 0.90 && serviceLevel < 0.95)
                {
                    safetyFactor = 1.28;
                }
                else if (serviceLevel >= 0.95 && serviceLevel < 0.99)
                {
                    safetyFactor = 1.65;
                }
                
                // For demo, we'll create recommendations for different item types
                var itemTypes = new[] { "A", "B", "C", "D" };
                var random = new Random(42); // For reproducibility
                
                foreach (var itemType in itemTypes)
                {
                    // Get current stock or generate a random value
                    int stock = currentStock.TryGetValue(itemType, out int value) ? 
                        value : random.Next(10, 50);
                    
                    // Calculate average daily demand from forecast
                    float avgDailyDemand = forecast.Average(r => r.Forecast / forecast.Count);
                    
                    // Calculate demand during lead time
                    float leadTimeDemand = avgDailyDemand * leadTime;
                    
                    // Calculate standard deviation of forecast error
                    float forecastErrorStdDev = forecast
                        .Select(f => f.UpperBound - f.Forecast)
                        .Average() / (float)safetyFactor;
                    
                    // Calculate safety stock
                    float safetyStock = (float)(safetyFactor * forecastErrorStdDev * Math.Sqrt(leadTime));
                    
                    // Calculate reorder point
                    float reorderPoint = leadTimeDemand + safetyStock;
                    
                    // Calculate recommended order quantity
                    int recommendedOrder = 0;
                    if (stock <= reorderPoint)
                    {
                        // If current stock is below reorder point, order enough to reach optimal level
                        recommendedOrder = (int)Math.Ceiling(reorderPoint + leadTimeDemand - stock);
                    }
                    
                    // Create recommendation
                    recommendations.Add(new InventoryRecommendation
                    {
                        Item = itemType,
                        CurrentStock = stock,
                        ReorderPoint = reorderPoint,
                        LeadTimeDemand = leadTimeDemand,
                        SafetyStock = safetyStock,
                        RecommendedOrder = recommendedOrder
                    });
                }
                
                return recommendations;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating inventory recommendations", ex);
                throw;
            }
        }
        
        /// <summary>
        /// Gets a list of available forecasting models
        /// </summary>
        /// <returns>List of model names</returns>
        public List<string> GetAvailableModels()
        {
            return _forecasterFactory.GetAvailableForecasters();
        }
        
        /// <summary>
        /// Generates demo forecast data when actual data is not available
        /// </summary>
        /// <param name="horizon">Number of periods to forecast</param>
        /// <returns>Demo forecast results</returns>
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> GenerateDemoForecast(int horizon)
        {
            var startDate = DateTime.Today;
            var random = new Random(42);
            var forecasts = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
            
            float baseValue = 100;
            
            for (int i = 0; i < horizon; i++)
            {
                var date = startDate.AddDays(i);
                var forecast = baseValue + i * 2 + (float)(random.NextDouble() * 5 - 2.5);
                var error = (float)(forecast * 0.1); // 10% error margin
                
                forecasts.Add((
                    date,
                    forecast,
                    forecast - error,
                    forecast + error
                ));
                
                baseValue = forecast; // Trend follows previous forecast
            }
            
            return forecasts;
        }
    }
}