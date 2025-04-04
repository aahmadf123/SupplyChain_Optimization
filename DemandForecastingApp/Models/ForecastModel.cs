using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;
using DemandForecastingApp.Data;

namespace DemandForecastingApp.Models
{
    public class ForecastModel
    {
        private readonly MLContext _mlContext;
        
        public ForecastModel()
        {
            _mlContext = new MLContext(seed: 0);
        }
        
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> PredictDemand(
            List<DemandRecord> historicalData, int horizon)
        {
            try
            {
                // Convert data to IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(historicalData);
                
                // Define SSA forecasting pipeline
                const int seriesLength = 30;  // Should be determined based on your data frequency and patterns
                const int windowSize = 5;     // Should be tuned for your specific data
                const int trainSize = 60;    // Amount of data to use for training
                
                // Create SSA forecasting transformer
                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedDemand",
                    inputColumnName: "Demand",
                    windowSize: windowSize, 
                    seriesLength: seriesLength,
                    trainSize: trainSize,
                    horizon: horizon,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: "LowerBound",
                    confidenceUpperBoundColumn: "UpperBound"
                );
                
                // Create the forecasting engine
                var forecaster = forecastingPipeline.Fit(dataView);
                
                // Forecast
                var forecast = forecaster.Transform(dataView);
                
                // Get the forecasted values
                var forecastOutput = _mlContext.Data.CreateEnumerable<ForecastOutput>(forecast, reuseRowObject: false).ToList();
                
                // Generate forecast results for future dates
                var lastDate = historicalData.Max(x => x.Date);
                var results = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
                
                for (int i = 0; i < horizon; i++)
                {
                    var forecastDate = lastDate.AddDays(i + 1); // Add days - adjust according to your data frequency
                    var forecastValue = forecastOutput.FirstOrDefault();
                    
                    results.Add((
                        Date: forecastDate, 
                        Forecast: forecastValue?.ForecastedDemand ?? 0,
                        LowerBound: forecastValue?.LowerBound ?? 0,
                        UpperBound: forecastValue?.UpperBound ?? 0
                    ));
                }
                
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Forecasting error: {ex}");
                return new List<(DateTime, float, float, float)>();
            }
        }
        
        private class ForecastOutput
        {
            public float Demand { get; set; }
            public float ForecastedDemand { get; set; }
            public float LowerBound { get; set; }
            public float UpperBound { get; set; }
        }
    }
}