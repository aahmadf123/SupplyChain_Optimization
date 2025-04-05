using System;
using System.Collections.Generic;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Factory class for creating and managing forecasting models
    /// </summary>
    public class ForecasterFactory
    {
        private readonly Dictionary<string, Type> _registeredForecasters;
        
        public ForecasterFactory()
        {
            _registeredForecasters = new Dictionary<string, Type>
            {
                { "SSA", typeof(SSAForecaster) },
                { "LSTM", typeof(LSTMForecaster) }
                // Add more forecasters as they're implemented
            };
        }
        
        /// <summary>
        /// Creates a forecaster instance based on the specified model type
        /// </summary>
        /// <param name="modelType">Type of forecasting model to create</param>
        /// <returns>IForecaster implementation</returns>
        public IForecaster CreateForecaster(string modelType)
        {
            try
            {
                // Default to SSA if model type is not specified or not found
                modelType = string.IsNullOrEmpty(modelType) ? "SSA" : modelType.ToUpper();
                
                // Match partial model names
                string matchedType = null;
                foreach (var key in _registeredForecasters.Keys)
                {
                    if (modelType.Contains(key))
                    {
                        matchedType = key;
                        break;
                    }
                }
                
                if (matchedType == null)
                {
                    Logger.LogWarning($"Forecaster type '{modelType}' not found. Defaulting to SSA.");
                    matchedType = "SSA";
                }
                
                var forecasterType = _registeredForecasters[matchedType];
                var forecaster = (IForecaster)Activator.CreateInstance(forecasterType);
                
                Logger.LogInfo($"Created forecaster of type {forecasterType.Name}");
                
                return forecaster;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error creating forecaster: {ex.Message}", ex);
                
                // Fall back to SSA forecaster
                Logger.LogInfo("Falling back to SSA forecaster");
                return new SSAForecaster();
            }
        }
        
        /// <summary>
        /// Gets a list of available forecaster types
        /// </summary>
        /// <returns>List of forecaster names</returns>
        public List<string> GetAvailableForecasters()
        {
            return new List<string>(_registeredForecasters.Keys);
        }
    }
}