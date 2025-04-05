using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Forecasting model using Long Short-Term Memory (LSTM) neural networks
    /// This is a simplified implementation that could be connected to an external ML framework
    /// </summary>
    public class LSTMForecaster : IForecaster
    {
        private int _lookbackWindow = 14;
        private int _numEpochs = 100;
        private bool _isTraining = false;
        private bool _isModelReady = false;
        private List<float> _trainedSeries = new List<float>();
        private float _meanValue = 0;
        private float _stdDevValue = 1;
        
        // In a real implementation, these would connect to a deep learning library
        // For now, we'll simulate the behavior for demonstration
        private List<double> _weights = new List<double>();
        
        /// <summary>
        /// Name of this forecasting model
        /// </summary>
        public string ModelName => "Long Short-Term Memory (LSTM)";

        /// <summary>
        /// Number of past days to use for making predictions
        /// </summary>
        public int LookbackWindow
        {
            get => _lookbackWindow;
            set => _lookbackWindow = Math.Max(7, Math.Min(value, 30)); // Constrain between 7 and 30
        }

        /// <summary>
        /// Number of training epochs
        /// </summary>
        public int NumEpochs
        {
            get => _numEpochs;
            set => _numEpochs = Math.Max(10, Math.Min(value, 500)); // Constrain between 10 and 500
        }

        /// <summary>
        /// Constructor with default parameters
        /// </summary>
        public LSTMForecaster()
        {
            // Load parameters from settings if available
            string lookbackSetting = AppSettings.GetSetting("LSTMLookback");
            if (!string.IsNullOrEmpty(lookbackSetting) && int.TryParse(lookbackSetting, out int lookback))
            {
                LookbackWindow = lookback;
            }

            string epochsSetting = AppSettings.GetSetting("LSTMEpochs");
            if (!string.IsNullOrEmpty(epochsSetting) && int.TryParse(epochsSetting, out int epochs))
            {
                NumEpochs = epochs;
            }
        }

        /// <summary>
        /// Trains the LSTM model using historical sales data
        /// </summary>
        /// <param name="trainingData">Historical sales data for training</param>
        /// <returns>True if training was successful</returns>
        public bool Train(List<RossmannSalesRecord> trainingData)
        {
            try
            {
                Logger.LogInfo($"Training LSTM model with {trainingData.Count} records, lookback window {_lookbackWindow}");
                
                if (trainingData == null || trainingData.Count < _lookbackWindow * 2)
                {
                    Logger.LogWarning("Not enough training data for LSTM model");
                    return false;
                }

                // Mark as training
                _isTraining = true;
                _isModelReady = false;
                
                // Extract the time series from the training data
                _trainedSeries = trainingData.Select(r => r.Sales).ToList();

                // Normalize the data
                NormalizeData();

                // Start async training
                Task.Run(() => TrainModelAsync());
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training LSTM model", ex);
                _isTraining = false;
                return false;
            }
        }

        /// <summary>
        /// Predicts future sales based on recent data using the trained LSTM model
        /// </summary>
        /// <param name="recentData">Recent sales data for making predictions</param>
        /// <param name="horizon">Number of periods to forecast into the future</param>
        /// <returns>List of forecasts with date, predicted value, and confidence intervals</returns>
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(
            List<RossmannSalesRecord> recentData, 
            int horizon = 14)
        {
            try
            {
                Logger.LogInfo($"Generating LSTM forecast for {horizon} periods ahead");
                
                // Check if model is ready
                if (!_isModelReady)
                {
                    Logger.LogWarning("LSTM model not ready yet. Current status: " + 
                        (_isTraining ? "Training in progress" : "Not trained"));
                    return new List<(DateTime, float, float, float)>();
                }

                // Get the most recent data points to use for prediction
                var recentSeries = recentData.Select(r => r.Sales).ToList();
                if (recentSeries.Count < _lookbackWindow)
                {
                    // If recent data is too short, pad with trained series data
                    recentSeries = _trainedSeries
                        .Skip(Math.Max(0, _trainedSeries.Count - (_lookbackWindow - recentSeries.Count)))
                        .Concat(recentSeries)
                        .ToList();
                }

                // Use the last lookback_window values to forecast
                var inputWindow = recentSeries
                    .Skip(Math.Max(0, recentSeries.Count - _lookbackWindow))
                    .Take(_lookbackWindow)
                    .ToList();
                
                // Normalize the input window
                var normalizedInput = inputWindow
                    .Select(x => (x - _meanValue) / _stdDevValue)
                    .ToList();

                // Generate forecasts one step at a time
                var forecasts = new List<(DateTime, float, float, float)>();
                DateTime lastDate = recentData.Last().Date;
                
                var forecastSeries = new List<float>(normalizedInput);
                for (int i = 0; i < horizon; i++)
                {
                    // Get the last window of data
                    var window = forecastSeries
                        .Skip(forecastSeries.Count - _lookbackWindow)
                        .Take(_lookbackWindow)
                        .ToArray();
                    
                    // Make one-step forecast
                    float normalizedForecast = ForecastOneStep(window);
                    
                    // Add to normalized series
                    forecastSeries.Add(normalizedForecast);
                    
                    // Denormalize the forecast
                    float forecast = normalizedForecast * _stdDevValue + _meanValue;
                    
                    // Calculate confidence intervals
                    float confidenceWidth = CalculateConfidenceInterval(i + 1);
                    
                    // Add to results with date
                    DateTime forecastDate = lastDate.AddDays(i + 1);
                    forecasts.Add((
                        forecastDate,
                        Math.Max(0, forecast),  // Ensure non-negative forecast
                        Math.Max(0, forecast - confidenceWidth),
                        forecast + confidenceWidth
                    ));
                }

                Logger.LogInfo($"LSTM forecast complete with {forecasts.Count} periods");
                return forecasts;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating LSTM forecast", ex);
                return new List<(DateTime, float, float, float)>();
            }
        }

        /// <summary>
        /// Evaluates the model's accuracy on test data
        /// </summary>
        /// <param name="testData">Test data to evaluate against</param>
        /// <returns>Mean Absolute Percentage Error (MAPE)</returns>
        public float Evaluate(List<RossmannSalesRecord> testData)
        {
            try
            {
                if (!_isModelReady || testData == null || testData.Count < _lookbackWindow + 1)
                {
                    return float.NaN;
                }

                // Use sliding window evaluation
                float totalError = 0;
                int validPredictions = 0;
                
                // Normalize test data
                var testSeries = testData.Select(r => r.Sales).ToList();
                var normalizedTestSeries = testSeries
                    .Select(x => (x - _meanValue) / _stdDevValue)
                    .ToList();

                for (int i = _lookbackWindow; i < normalizedTestSeries.Count; i++)
                {
                    // Get the input window
                    var window = normalizedTestSeries
                        .Skip(i - _lookbackWindow)
                        .Take(_lookbackWindow)
                        .ToArray();
                    
                    // Make prediction
                    float normalizedPrediction = ForecastOneStep(window);
                    
                    // Denormalize
                    float prediction = normalizedPrediction * _stdDevValue + _meanValue;
                    float actual = testSeries[i];
                    
                    // Calculate absolute percentage error
                    if (actual > 0)
                    {
                        float error = Math.Abs((prediction - actual) / actual);
                        totalError += error;
                        validPredictions++;
                    }
                }

                // Return Mean Absolute Percentage Error
                return validPredictions > 0 ? (totalError / validPredictions) : float.NaN;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error evaluating LSTM model", ex);
                return float.NaN;
            }
        }

        /// <summary>
        /// Normalizes the training data using z-score normalization
        /// </summary>
        private void NormalizeData()
        {
            // Calculate mean and standard deviation
            _meanValue = _trainedSeries.Average();
            
            float sumSquaredDiff = 0;
            foreach (var value in _trainedSeries)
            {
                float diff = value - _meanValue;
                sumSquaredDiff += diff * diff;
            }
            
            _stdDevValue = (float)Math.Sqrt(sumSquaredDiff / _trainedSeries.Count);
            
            // Prevent division by zero
            if (_stdDevValue < 0.1f)
                _stdDevValue = 0.1f;
        }

        /// <summary>
        /// Asynchronous model training process
        /// </summary>
        private async Task TrainModelAsync()
        {
            try
            {
                // In a real implementation, this would connect to a deep learning library
                // For now, we'll simulate the LSTM training process
                
                // Simulate initialization of weights
                Random random = new Random(42);
                _weights.Clear();
                
                // Add random weights (in a real model, these would be learned)
                // We'll use these weights in ForecastOneStep
                int numWeights = _lookbackWindow * 4; // Simplified representation
                for (int i = 0; i < numWeights; i++)
                {
                    _weights.Add((random.NextDouble() - 0.5) * 0.1);
                }

                // Simulate training epochs
                for (int epoch = 0; epoch < _numEpochs; epoch++)
                {
                    // Simulate a training step with a slight delay
                    await Task.Delay(50);
                    
                    // In a real implementation, we would update the weights here
                    // For now, we'll just adjust them slightly to simulate learning
                    for (int i = 0; i < _weights.Count; i++)
                    {
                        _weights[i] += (random.NextDouble() - 0.5) * 0.001;
                    }
                    
                    if (epoch % 10 == 0)
                    {
                        Logger.LogInfo($"LSTM training: epoch {epoch}/{_numEpochs}");
                    }
                }

                // Mark model as ready
                _isModelReady = true;
                _isTraining = false;
                Logger.LogInfo("LSTM model training completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during LSTM model training", ex);
                _isModelReady = false;
                _isTraining = false;
            }
        }

        /// <summary>
        /// Forecasts one step ahead based on a window of recent normalized values
        /// </summary>
        /// <param name="window">Recent window of normalized values</param>
        /// <returns>Forecasted normalized value</returns>
        private float ForecastOneStep(float[] window)
        {
            // In a real implementation, this would use the actual LSTM model
            // For now, we'll simulate with a simple weighted average
            
            if (_weights.Count < window.Length)
                return window.Last(); // Fallback to last value prediction
            
            float prediction = 0;
            
            // More weight to recent values
            for (int i = 0; i < window.Length; i++)
            {
                // Convert index to 0-1 range for weighting (more recent = higher weight)
                float normalizedIdx = (float)i / window.Length;
                
                // Use sigmoid to give more weight to recent values
                float weight = (float)(1.0 / (1.0 + Math.Exp(-10 * (normalizedIdx - 0.5))));
                
                // Each value gets its own learned weight component
                float modelWeight = (float)_weights[i % _weights.Count];
                
                prediction += window[i] * (weight + modelWeight);
            }
            
            // Add a small trend component based on the last few values
            if (window.Length >= 3)
            {
                float recentTrend = window[window.Length - 1] - window[window.Length - 3];
                prediction += recentTrend * 0.2f;
            }
            
            // Apply non-linearity to prevent extreme values
            prediction = (float)Math.Tanh(prediction * 0.5) * 2;
            
            return prediction;
        }

        /// <summary>
        /// Calculates the confidence interval width based on forecast horizon
        /// </summary>
        /// <param name="horizon">How many steps ahead the forecast is</param>
        /// <returns>Confidence interval width</returns>
        private float CalculateConfidenceInterval(int horizon)
        {
            // Base uncertainty
            float baseUncertainty = _stdDevValue * 0.5f;
            
            // Uncertainty grows with the horizon (square root relationship)
            float growthFactor = (float)Math.Sqrt(horizon) / (float)Math.Sqrt(7);
            
            // Return the confidence interval width
            return baseUncertainty * (1 + growthFactor);
        }
    }
}