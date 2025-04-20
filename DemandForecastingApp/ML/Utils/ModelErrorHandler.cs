using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Handles model errors and implements recovery strategies
    /// </summary>
    public class ModelErrorHandler
    {
        private readonly string _modelName;
        private readonly int _maxRetries;
        private readonly TimeSpan _retryDelay;
        private readonly Dictionary<string, int> _errorCounts;
        private readonly List<ErrorRecord> _errorHistory;
        private readonly int _maxErrorHistory;

        public ModelErrorHandler(
            string modelName,
            int maxRetries = 3,
            TimeSpan? retryDelay = null,
            int maxErrorHistory = 100)
        {
            _modelName = modelName;
            _maxRetries = maxRetries;
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
            _errorCounts = new Dictionary<string, int>();
            _errorHistory = new List<ErrorRecord>();
            _maxErrorHistory = maxErrorHistory;
        }

        /// <summary>
        /// Handles training errors with recovery strategies
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="model">The model that failed</param>
        /// <param name="data">The training data</param>
        /// <returns>True if recovery was successful</returns>
        public bool HandleTrainingError(Exception ex, IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                string errorType = ex.GetType().Name;
                LogError(errorType, ex.Message, "Training");

                // Check if we've exceeded retry limit for this error type
                if (!_errorCounts.ContainsKey(errorType))
                {
                    _errorCounts[errorType] = 0;
                }

                if (_errorCounts[errorType] >= _maxRetries)
                {
                    Logger.LogError($"Maximum retries exceeded for {errorType} during training of {_modelName}");
                    return false;
                }

                _errorCounts[errorType]++;

                // Implement recovery strategies based on error type
                bool recovered = false;
                switch (errorType)
                {
                    case "ArgumentException":
                        recovered = HandleArgumentException(model, data);
                        break;

                    case "InvalidOperationException":
                        recovered = HandleInvalidOperationException(model, data);
                        break;

                    case "OutOfMemoryException":
                        recovered = HandleOutOfMemoryException(model, data);
                        break;

                    default:
                        recovered = HandleGenericError(model, data);
                        break;
                }

                if (recovered)
                {
                    Logger.LogInfo($"Successfully recovered from {errorType} during training of {_modelName}");
                    _errorCounts[errorType] = 0; // Reset error count on successful recovery
                }

                return recovered;
            }
            catch (Exception handlerEx)
            {
                Logger.LogError($"Error in error handler for {_modelName}", handlerEx);
                return false;
            }
        }

        /// <summary>
        /// Handles prediction errors with recovery strategies
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="model">The model that failed</param>
        /// <param name="data">The prediction data</param>
        /// <returns>True if recovery was successful</returns>
        public bool HandlePredictionError(Exception ex, IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                string errorType = ex.GetType().Name;
                LogError(errorType, ex.Message, "Prediction");

                // For prediction errors, we might want to fall back to simpler models
                switch (errorType)
                {
                    case "ArgumentException":
                        return HandlePredictionArgumentException(model, data);

                    case "InvalidOperationException":
                        return HandlePredictionInvalidOperationException(model, data);

                    default:
                        return HandleGenericPredictionError(model, data);
                }
            }
            catch (Exception handlerEx)
            {
                Logger.LogError($"Error in prediction error handler for {_modelName}", handlerEx);
                return false;
            }
        }

        /// <summary>
        /// Gets the error history
        /// </summary>
        /// <returns>List of error records</returns>
        public List<ErrorRecord> GetErrorHistory()
        {
            return new List<ErrorRecord>(_errorHistory);
        }

        /// <summary>
        /// Gets error statistics
        /// </summary>
        /// <returns>Dictionary of error types and their counts</returns>
        public Dictionary<string, int> GetErrorStatistics()
        {
            return new Dictionary<string, int>(_errorCounts);
        }

        private void LogError(string errorType, string message, string operation)
        {
            var errorRecord = new ErrorRecord
            {
                Timestamp = DateTime.UtcNow,
                ErrorType = errorType,
                Message = message,
                Operation = operation
            };

            _errorHistory.Add(errorRecord);
            while (_errorHistory.Count > _maxErrorHistory)
            {
                _errorHistory.RemoveAt(0);
            }

            Logger.LogError($"Error in {operation} for {_modelName}: {errorType} - {message}");
        }

        private bool HandleArgumentException(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Try to clean and validate the data
                var cleanedData = data.Where(r => r.Sales >= 0 && !float.IsNaN(r.Sales)).ToList();
                if (cleanedData.Count < data.Count * 0.9) // If we lost more than 10% of data
                {
                    Logger.LogWarning("Too much data would be lost during cleaning");
                    return false;
                }

                // Retry training with cleaned data
                return model.Train(cleanedData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in argument exception handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandleInvalidOperationException(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Try to reinitialize the model
                if (model is SSAForecaster ssaModel)
                {
                    ssaModel.WindowSize = 7; // Reset to default
                    ssaModel.NumComponents = 3;
                }
                else if (model is LSTMForecaster lstmModel)
                {
                    lstmModel.LookbackWindow = 14;
                    lstmModel.NumEpochs = 100;
                }

                // Retry training
                return model.Train(data);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in invalid operation exception handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandleOutOfMemoryException(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Try to reduce memory usage by using a subset of data
                var reducedData = data.OrderByDescending(r => r.Date)
                    .Take(1000) // Use only the most recent 1000 records
                    .OrderBy(r => r.Date)
                    .ToList();

                return model.Train(reducedData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in out of memory exception handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandleGenericError(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Wait before retrying
                System.Threading.Thread.Sleep(_retryDelay);
                return model.Train(data);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in generic error handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandlePredictionArgumentException(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Try to clean the prediction data
                var cleanedData = data.Where(r => r.Sales >= 0 && !float.IsNaN(r.Sales)).ToList();
                if (cleanedData.Count == 0)
                {
                    return false;
                }

                // Make prediction with cleaned data
                var predictions = model.Predict(cleanedData, 1);
                return predictions.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in prediction argument exception handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandlePredictionInvalidOperationException(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Try to retrain the model with recent data
                var recentData = data.OrderByDescending(r => r.Date)
                    .Take(30)
                    .OrderBy(r => r.Date)
                    .ToList();

                if (!model.Train(recentData))
                {
                    return false;
                }

                // Make prediction
                var predictions = model.Predict(data, 1);
                return predictions.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in prediction invalid operation exception handler for {_modelName}", ex);
                return false;
            }
        }

        private bool HandleGenericPredictionError(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                // Wait before retrying
                System.Threading.Thread.Sleep(_retryDelay);
                var predictions = model.Predict(data, 1);
                return predictions.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in generic prediction error handler for {_modelName}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Contains information about a model error
    /// </summary>
    public class ErrorRecord
    {
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; }
        public string Message { get; set; }
        public string Operation { get; set; }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} - {Operation} Error: {ErrorType} - {Message}";
        }
    }
} 