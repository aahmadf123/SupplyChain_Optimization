using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Combines predictions from multiple forecasting models
    /// </summary>
    public class EnsembleForecaster : IForecaster
    {
        private readonly List<IForecaster> _models;
        private readonly List<float> _weights;
        private readonly string _ensembleName;
        private bool _isTrained;

        public string ModelName => _ensembleName;

        public EnsembleForecaster(string ensembleName = "Ensemble")
        {
            _models = new List<IForecaster>();
            _weights = new List<float>();
            _ensembleName = ensembleName;
            _isTrained = false;
        }

        /// <summary>
        /// Adds a model to the ensemble
        /// </summary>
        /// <param name="model">The model to add</param>
        /// <param name="weight">Optional weight for the model (defaults to equal weighting)</param>
        public void AddModel(IForecaster model, float? weight = null)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            _models.Add(model);
            _weights.Add(weight ?? 1.0f);
            _isTrained = false;
        }

        /// <summary>
        /// Trains all models in the ensemble
        /// </summary>
        /// <param name="trainingData">Historical sales data for training</param>
        /// <returns>True if all models were trained successfully</returns>
        public bool Train(List<RossmannSalesRecord> trainingData)
        {
            try
            {
                Logger.LogInfo($"Training ensemble with {_models.Count} models");

                if (_models.Count == 0)
                {
                    Logger.LogWarning("No models in ensemble to train");
                    return false;
                }

                bool allSuccess = true;
                for (int i = 0; i < _models.Count; i++)
                {
                    Logger.LogInfo($"Training model {i + 1}/{_models.Count}: {_models[i].ModelName}");
                    if (!_models[i].Train(trainingData))
                    {
                        Logger.LogWarning($"Failed to train model {_models[i].ModelName}");
                        allSuccess = false;
                    }
                }

                if (allSuccess)
                {
                    _isTrained = true;
                    Logger.LogInfo("All models in ensemble trained successfully");
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training ensemble", ex);
                return false;
            }
        }

        /// <summary>
        /// Generates predictions by combining outputs from all models
        /// </summary>
        /// <param name="recentData">Recent sales data for making predictions</param>
        /// <param name="horizon">Number of periods to forecast into the future</param>
        /// <returns>Ensemble forecasts with date, predicted value, and confidence intervals</returns>
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(
            List<RossmannSalesRecord> recentData,
            int horizon = 10)
        {
            try
            {
                if (!_isTrained)
                {
                    Logger.LogWarning("Ensemble not trained yet");
                    return new List<(DateTime, float, float, float)>();
                }

                Logger.LogInfo($"Generating ensemble forecast for {horizon} periods ahead");

                // Get predictions from all models
                var allPredictions = new List<List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>>();
                foreach (var model in _models)
                {
                    var predictions = model.Predict(recentData, horizon);
                    if (predictions.Count > 0)
                    {
                        allPredictions.Add(predictions);
                    }
                }

                if (allPredictions.Count == 0)
                {
                    Logger.LogWarning("No valid predictions from any model in ensemble");
                    return new List<(DateTime, float, float, float)>();
                }

                // Normalize weights
                float totalWeight = _weights.Take(allPredictions.Count).Sum();
                var normalizedWeights = _weights.Take(allPredictions.Count)
                    .Select(w => w / totalWeight)
                    .ToList();

                // Combine predictions
                var ensemblePredictions = new List<(DateTime, float, float, float)>();
                for (int i = 0; i < horizon; i++)
                {
                    float weightedForecast = 0;
                    float weightedLowerBound = 0;
                    float weightedUpperBound = 0;
                    DateTime date = allPredictions[0][i].Date;

                    for (int j = 0; j < allPredictions.Count; j++)
                    {
                        var prediction = allPredictions[j][i];
                        float weight = normalizedWeights[j];

                        weightedForecast += prediction.Forecast * weight;
                        weightedLowerBound += prediction.LowerBound * weight;
                        weightedUpperBound += prediction.UpperBound * weight;
                    }

                    ensemblePredictions.Add((date, weightedForecast, weightedLowerBound, weightedUpperBound));
                }

                Logger.LogInfo($"Ensemble forecast complete with {ensemblePredictions.Count} periods");
                return ensemblePredictions;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating ensemble forecast", ex);
                return new List<(DateTime, float, float, float)>();
            }
        }

        /// <summary>
        /// Evaluates the ensemble's performance on test data
        /// </summary>
        /// <param name="testData">Test data to evaluate against</param>
        /// <returns>Mean Absolute Percentage Error (MAPE)</returns>
        public float Evaluate(List<RossmannSalesRecord> testData)
        {
            try
            {
                if (!_isTrained || testData == null || testData.Count == 0)
                {
                    return float.NaN;
                }

                // Get predictions for the test period
                var predictions = Predict(testData.Take(testData.Count - 1).ToList(), 1);
                if (predictions.Count == 0)
                {
                    return float.NaN;
                }

                // Calculate MAPE
                float totalError = 0;
                int validPredictions = 0;

                for (int i = 0; i < predictions.Count; i++)
                {
                    float actual = testData[i + 1].Sales;
                    float predicted = predictions[i].Forecast;

                    if (actual > 0)
                    {
                        float error = Math.Abs((predicted - actual) / actual);
                        totalError += error;
                        validPredictions++;
                    }
                }

                return validPredictions > 0 ? (totalError / validPredictions) : float.NaN;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error evaluating ensemble", ex);
                return float.NaN;
            }
        }

        /// <summary>
        /// Updates model weights based on their individual performance
        /// </summary>
        /// <param name="validationData">Data to use for weight adjustment</param>
        public void UpdateWeights(List<RossmannSalesRecord> validationData)
        {
            try
            {
                if (!_isTrained || validationData == null || validationData.Count == 0)
                {
                    return;
                }

                Logger.LogInfo("Updating ensemble weights based on model performance");

                var performances = new List<float>();
                for (int i = 0; i < _models.Count; i++)
                {
                    float mape = _models[i].Evaluate(validationData);
                    if (!float.IsNaN(mape))
                    {
                        // Convert MAPE to a score (lower MAPE = higher score)
                        float score = 1.0f / (1.0f + mape);
                        performances.Add(score);
                    }
                    else
                    {
                        performances.Add(0);
                    }
                }

                // Normalize scores to weights
                float totalScore = performances.Sum();
                if (totalScore > 0)
                {
                    for (int i = 0; i < _weights.Count; i++)
                    {
                        _weights[i] = performances[i] / totalScore;
                    }
                }

                Logger.LogInfo($"Updated weights: {string.Join(", ", _weights.Select((w, i) => $"{_models[i].ModelName}={w:F4}"))}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error updating ensemble weights", ex);
            }
        }
    }
} 