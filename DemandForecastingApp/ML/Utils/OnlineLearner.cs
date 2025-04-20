using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Supports online learning and model drift detection for forecasting models
    /// </summary>
    public class OnlineLearner
    {
        private readonly IForecaster _model;
        private readonly int _windowSize;
        private readonly Queue<RossmannSalesRecord> _recentData;
        private readonly List<float> _historicalErrors;
        private readonly float _driftThreshold;
        private DateTime _lastUpdateTime;
        private float _lastPrediction;

        public OnlineLearner(IForecaster model, int windowSize = 30, float driftThreshold = 0.1f)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _windowSize = Math.Max(7, Math.Min(windowSize, 90)); // Constrain between 7 and 90 days
            _driftThreshold = driftThreshold;
            _recentData = new Queue<RossmannSalesRecord>();
            _historicalErrors = new List<float>();
            _lastUpdateTime = DateTime.MinValue;
            _lastPrediction = float.NaN;
        }

        /// <summary>
        /// Updates the model with new data
        /// </summary>
        /// <param name="newData">New sales record</param>
        /// <returns>True if update was successful</returns>
        public bool UpdateModel(RossmannSalesRecord newData)
        {
            try
            {
                if (newData == null)
                {
                    Logger.LogWarning("Received null data for model update");
                    return false;
                }

                Logger.LogInfo($"Updating model with new data from {newData.Date:yyyy-MM-dd}");

                // Add to recent data window
                _recentData.Enqueue(newData);
                while (_recentData.Count > _windowSize)
                {
                    _recentData.Dequeue();
                }

                // Calculate error if we have a previous prediction
                if (!float.IsNaN(_lastPrediction) && _lastUpdateTime == newData.Date.AddDays(-1))
                {
                    float error = Math.Abs((_lastPrediction - newData.Sales) / newData.Sales);
                    _historicalErrors.Add(error);

                    // Keep only recent errors
                    while (_historicalErrors.Count > _windowSize)
                    {
                        _historicalErrors.RemoveAt(0);
                    }
                }

                // Retrain model if we have enough data
                if (_recentData.Count >= _windowSize)
                {
                    var trainingData = _recentData.ToList();
                    if (!_model.Train(trainingData))
                    {
                        Logger.LogWarning("Failed to update model with new data");
                        return false;
                    }
                }

                _lastUpdateTime = newData.Date;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error updating model", ex);
                return false;
            }
        }

        /// <summary>
        /// Makes a prediction for the next period
        /// </summary>
        /// <param name="horizon">Number of periods to forecast</param>
        /// <returns>Forecast with confidence intervals</returns>
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(int horizon = 1)
        {
            try
            {
                if (_recentData.Count < _windowSize)
                {
                    Logger.LogWarning($"Insufficient data for prediction. Need {_windowSize} records, have {_recentData.Count}");
                    return new List<(DateTime, float, float, float)>();
                }

                var recentData = _recentData.ToList();
                var predictions = _model.Predict(recentData, horizon);

                if (predictions.Count > 0)
                {
                    _lastPrediction = predictions[0].Forecast;
                }

                return predictions;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error making prediction", ex);
                return new List<(DateTime, float, float, float)>();
            }
        }

        /// <summary>
        /// Detects if the model has drifted from recent data
        /// </summary>
        /// <returns>True if significant drift is detected</returns>
        public bool HasModelDrifted()
        {
            try
            {
                if (_historicalErrors.Count < _windowSize)
                {
                    return false;
                }

                // Calculate recent error statistics
                float recentMeanError = _historicalErrors.TakeLast(_windowSize / 2).Average();
                float olderMeanError = _historicalErrors.Take(_windowSize / 2).Average();

                // Calculate drift as relative change in error
                float drift = Math.Abs((recentMeanError - olderMeanError) / olderMeanError);

                Logger.LogInfo($"Model drift detected: {drift:F4} (threshold: {_driftThreshold})");
                return drift > _driftThreshold;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error detecting model drift", ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the current model performance metrics
        /// </summary>
        /// <returns>Performance metrics including mean error and drift</returns>
        public ModelPerformance GetModelPerformance()
        {
            try
            {
                var performance = new ModelPerformance
                {
                    NumSamples = _historicalErrors.Count,
                    MeanError = _historicalErrors.Count > 0 ? _historicalErrors.Average() : float.NaN,
                    StandardDeviation = CalculateStandardDeviation(_historicalErrors),
                    LastUpdateTime = _lastUpdateTime
                };

                if (_historicalErrors.Count >= _windowSize)
                {
                    float recentMeanError = _historicalErrors.TakeLast(_windowSize / 2).Average();
                    float olderMeanError = _historicalErrors.Take(_windowSize / 2).Average();
                    performance.Drift = Math.Abs((recentMeanError - olderMeanError) / olderMeanError);
                }

                return performance;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error calculating model performance", ex);
                return new ModelPerformance();
            }
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            if (values.Count < 2)
                return float.NaN;

            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
            return (float)Math.Sqrt(sumSquaredDiffs / (values.Count - 1));
        }
    }

    /// <summary>
    /// Contains model performance metrics
    /// </summary>
    public class ModelPerformance
    {
        public int NumSamples { get; set; }
        public float MeanError { get; set; }
        public float StandardDeviation { get; set; }
        public float Drift { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public override string ToString()
        {
            return $"Model Performance:\n" +
                   $"Samples: {NumSamples}\n" +
                   $"Mean Error: {MeanError:F4}\n" +
                   $"Standard Deviation: {StandardDeviation:F4}\n" +
                   $"Drift: {Drift:F4}\n" +
                   $"Last Update: {LastUpdateTime:yyyy-MM-dd HH:mm:ss}";
        }
    }
} 