using System;
using System.Collections.Generic;
using DemandForecastingApp.Models;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Interface for all forecasting models
    /// </summary>
    public interface IForecaster
    {
        /// <summary>
        /// Name of the forecasting model
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Trains the forecaster using historical sales data
        /// </summary>
        /// <param name="trainingData">Historical sales data for training</param>
        /// <returns>True if training was successful</returns>
        bool Train(List<RossmannSalesRecord> trainingData);
        
        /// <summary>
        /// Predicts future sales based on recent data
        /// </summary>
        /// <param name="recentData">Recent sales data for making predictions</param>
        /// <param name="horizon">Number of periods to forecast into the future</param>
        /// <returns>List of forecasts with date, predicted value, and confidence intervals</returns>
        List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(
            List<RossmannSalesRecord> recentData, 
            int horizon = 10);
            
        /// <summary>
        /// Evaluates the model's accuracy on test data
        /// </summary>
        /// <param name="testData">Test data to evaluate against</param>
        /// <returns>Error metric (typically MAPE - Mean Absolute Percentage Error)</returns>
        float Evaluate(List<RossmannSalesRecord> testData);
    }
}