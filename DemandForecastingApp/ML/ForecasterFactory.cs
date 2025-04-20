using System;
using System.Collections.Generic;
using DemandForecastingApp.ML.Utils;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Factory class for creating and managing forecasting models
    /// </summary>
    public class ForecasterFactory
    {
        private readonly Dictionary<string, IForecaster> _forecasters;
        private readonly Dictionary<string, ModelErrorHandler> _errorHandlers;
        private readonly Dictionary<string, ModelMonitor> _modelMonitors;
        
        public ForecasterFactory()
        {
            _forecasters = new Dictionary<string, IForecaster>();
            _errorHandlers = new Dictionary<string, ModelErrorHandler>();
            _modelMonitors = new Dictionary<string, ModelMonitor>();
        }
        
        /// <summary>
        /// Creates a forecaster instance based on the specified model type
        /// </summary>
        /// <param name="modelType">Type of forecasting model to create</param>
        /// <returns>IForecaster implementation</returns>
        public IForecaster GetForecaster(string modelType)
        {
            if (_forecasters.TryGetValue(modelType, out var existingForecaster))
            {
                return existingForecaster;
            }

            IForecaster forecaster = modelType.ToLower() switch
            {
                "ssa" => new SSAForecaster(),
                "lstm" => new LSTMForecaster(),
                _ => throw new ArgumentException($"Unknown model type: {modelType}")
            };

            // Create error handler for the forecaster
            var errorHandler = new ModelErrorHandler(modelType);
            _errorHandlers[modelType] = errorHandler;

            // Create model monitor for the forecaster
            var modelMonitor = new ModelMonitor(modelType);
            _modelMonitors[modelType] = modelMonitor;

            // Wrap the forecaster with error handling and monitoring
            var wrappedForecaster = new MonitoredForecaster(forecaster, errorHandler, modelMonitor);
            _forecasters[modelType] = wrappedForecaster;

            return wrappedForecaster;
        }
        
        /// <summary>
        /// Gets a list of available forecaster types
        /// </summary>
        /// <returns>List of forecaster names</returns>
        public List<string> GetAvailableForecasters()
        {
            return new List<string>(_forecasters.Keys);
        }

        public ModelErrorHandler GetErrorHandler(string modelType)
        {
            return _errorHandlers.TryGetValue(modelType, out var handler) ? handler : null;
        }

        public ModelMonitor GetModelMonitor(string modelType)
        {
            return _modelMonitors.TryGetValue(modelType, out var monitor) ? monitor : null;
        }
    }

    /// <summary>
    /// Wrapper class that adds error handling and monitoring to any forecaster
    /// </summary>
    public class MonitoredForecaster : IForecaster
    {
        private readonly IForecaster _forecaster;
        private readonly ModelErrorHandler _errorHandler;
        private readonly ModelMonitor _modelMonitor;

        public string ModelName => _forecaster.ModelName;

        public MonitoredForecaster(IForecaster forecaster, ModelErrorHandler errorHandler, ModelMonitor modelMonitor)
        {
            _forecaster = forecaster;
            _errorHandler = errorHandler;
            _modelMonitor = modelMonitor;
        }

        public bool Train(List<RossmannSalesRecord> data)
        {
            try
            {
                return _forecaster.Train(data);
            }
            catch (Exception ex)
            {
                return _errorHandler.HandleTrainingError(ex, _forecaster, data);
            }
        }

        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(
            List<RossmannSalesRecord> data, 
            int horizon = 10)
        {
            try
            {
                var predictions = _forecaster.Predict(data, horizon);
                _modelMonitor.TrackPrediction(predictions[0].Forecast, data[^1].Sales);
                return predictions;
            }
            catch (Exception ex)
            {
                if (_errorHandler.HandlePredictionError(ex, _forecaster, data))
                {
                    var predictions = _forecaster.Predict(data, horizon);
                    _modelMonitor.TrackPrediction(predictions[0].Forecast, data[^1].Sales);
                    return predictions;
                }
                throw;
            }
        }

        public float Evaluate(List<RossmannSalesRecord> testData)
        {
            try
            {
                return _forecaster.Evaluate(testData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error evaluating model {ModelName}", ex);
                return float.MaxValue; // Return worst possible score on error
            }
        }

        public string GetModelInfo()
        {
            return _forecaster.GetModelInfo();
        }
    }
}