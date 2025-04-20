using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Tracks and monitors model performance over time
    /// </summary>
    public class ModelMonitor
    {
        private readonly string _modelName;
        private readonly int _maxHistorySize;
        private readonly Queue<PredictionRecord> _predictionHistory;
        private readonly List<PerformanceMetric> _performanceHistory;
        private readonly float _performanceThreshold;
        private DateTime _lastAlertTime;
        private readonly TimeSpan _alertCooldown;

        public ModelMonitor(
            string modelName,
            int maxHistorySize = 1000,
            float performanceThreshold = 0.2f,
            TimeSpan? alertCooldown = null)
        {
            _modelName = modelName;
            _maxHistorySize = maxHistorySize;
            _performanceThreshold = performanceThreshold;
            _predictionHistory = new Queue<PredictionRecord>();
            _performanceHistory = new List<PerformanceMetric>();
            _lastAlertTime = DateTime.MinValue;
            _alertCooldown = alertCooldown ?? TimeSpan.FromHours(24);
        }

        /// <summary>
        /// Records a prediction and its actual outcome
        /// </summary>
        /// <param name="date">Date of the prediction</param>
        /// <param name="predicted">Predicted value</param>
        /// <param name="actual">Actual value</param>
        public void TrackPrediction(DateTime date, float predicted, float actual)
        {
            try
            {
                var record = new PredictionRecord
                {
                    Date = date,
                    PredictedValue = predicted,
                    ActualValue = actual,
                    Error = Math.Abs((predicted - actual) / actual),
                    Timestamp = DateTime.UtcNow
                };

                _predictionHistory.Enqueue(record);
                while (_predictionHistory.Count > _maxHistorySize)
                {
                    _predictionHistory.Dequeue();
                }

                // Check for performance degradation
                CheckPerformanceDegradation();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error tracking prediction for {_modelName}", ex);
            }
        }

        /// <summary>
        /// Gets the current performance metrics
        /// </summary>
        /// <returns>Current model performance metrics</returns>
        public ModelPerformance GetPerformanceMetrics()
        {
            try
            {
                if (_predictionHistory.Count == 0)
                {
                    return new ModelPerformance();
                }

                var recentPredictions = _predictionHistory.TakeLast(30).ToList();
                var performance = new ModelPerformance
                {
                    NumSamples = recentPredictions.Count,
                    MeanError = recentPredictions.Average(r => r.Error),
                    StandardDeviation = CalculateStandardDeviation(recentPredictions.Select(r => r.Error).ToList()),
                    LastUpdateTime = recentPredictions.Last().Timestamp
                };

                // Calculate drift
                if (recentPredictions.Count >= 15)
                {
                    var recentErrors = recentPredictions.TakeLast(15).Select(r => r.Error).ToList();
                    var olderErrors = recentPredictions.Take(15).Select(r => r.Error).ToList();
                    performance.Drift = Math.Abs((recentErrors.Average() - olderErrors.Average()) / olderErrors.Average());
                }

                // Store performance metric
                _performanceHistory.Add(new PerformanceMetric
                {
                    Timestamp = DateTime.UtcNow,
                    MeanError = performance.MeanError,
                    Drift = performance.Drift
                });

                // Keep only last 30 days of performance history
                while (_performanceHistory.Count > 30)
                {
                    _performanceHistory.RemoveAt(0);
                }

                return performance;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error calculating performance metrics for {_modelName}", ex);
                return new ModelPerformance();
            }
        }

        /// <summary>
        /// Gets the performance history
        /// </summary>
        /// <returns>List of historical performance metrics</returns>
        public List<PerformanceMetric> GetPerformanceHistory()
        {
            return new List<PerformanceMetric>(_performanceHistory);
        }

        /// <summary>
        /// Gets the prediction history
        /// </summary>
        /// <returns>List of historical predictions</returns>
        public List<PredictionRecord> GetPredictionHistory()
        {
            return new List<PredictionRecord>(_predictionHistory);
        }

        private void CheckPerformanceDegradation()
        {
            try
            {
                var performance = GetPerformanceMetrics();
                if (performance.NumSamples < 30)
                {
                    return; // Need more data for reliable performance assessment
                }

                // Check if performance has degraded beyond threshold
                if (performance.MeanError > _performanceThreshold)
                {
                    // Check if enough time has passed since last alert
                    if (DateTime.UtcNow - _lastAlertTime > _alertCooldown)
                    {
                        Logger.LogWarning($"Performance degradation detected for {_modelName}:\n" +
                                        $"Mean Error: {performance.MeanError:F4} (threshold: {_performanceThreshold:F4})\n" +
                                        $"Drift: {performance.Drift:F4}");
                        
                        _lastAlertTime = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking performance degradation for {_modelName}", ex);
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
    /// Contains information about a single prediction
    /// </summary>
    public class PredictionRecord
    {
        public DateTime Date { get; set; }
        public float PredictedValue { get; set; }
        public float ActualValue { get; set; }
        public float Error { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd}: Predicted={PredictedValue:F2}, Actual={ActualValue:F2}, Error={Error:F4}";
        }
    }

    /// <summary>
    /// Contains performance metrics for a specific time period
    /// </summary>
    public class PerformanceMetric
    {
        public DateTime Timestamp { get; set; }
        public float MeanError { get; set; }
        public float Drift { get; set; }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss}: Mean Error={MeanError:F4}, Drift={Drift:F4}";
        }
    }
} 