using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML
{
    /// <summary>
    /// Forecasting model using Singular Spectrum Analysis (SSA) technique
    /// </summary>
    public class SSAForecaster : IForecaster
    {
        private int _windowSize = 7;
        private int _numComponents = 3;
        private List<double[]> _eigenvectors;
        private double[] _means;
        private List<float> _trainedSeries;

        /// <summary>
        /// Name of this forecasting model
        /// </summary>
        public string ModelName => "Singular Spectrum Analysis (SSA)";

        /// <summary>
        /// Sets the window size (L) for SSA decomposition
        /// </summary>
        public int WindowSize
        {
            get => _windowSize;
            set => _windowSize = Math.Max(2, Math.Min(value, 30)); // Constrain between 2 and 30
        }

        /// <summary>
        /// Sets the number of components to use for reconstruction
        /// </summary>
        public int NumComponents
        {
            get => _numComponents;
            set => _numComponents = Math.Max(1, Math.Min(value, _windowSize)); // Can't exceed window size
        }

        /// <summary>
        /// Constructor with default parameters
        /// </summary>
        public SSAForecaster()
        {
            // Load window size from settings if available
            string windowSizeSetting = AppSettings.GetSetting("WindowSize");
            if (!string.IsNullOrEmpty(windowSizeSetting) && int.TryParse(windowSizeSetting, out int windowSize))
            {
                WindowSize = windowSize;
            }
        }

        /// <summary>
        /// Trains the SSA model using historical sales data
        /// </summary>
        /// <param name="trainingData">Historical sales data for training</param>
        /// <returns>True if training was successful</returns>
        public bool Train(List<RossmannSalesRecord> trainingData)
        {
            try
            {
                Logger.LogInfo($"Training SSA model with {trainingData.Count} records, window size {_windowSize}");
                
                if (trainingData == null || trainingData.Count < _windowSize * 2)
                {
                    Logger.LogWarning("Not enough training data for SSA model");
                    return false;
                }

                // Extract the time series from the training data
                _trainedSeries = trainingData.Select(r => r.Sales).ToList();
                
                // Convert to double array for numerical stability
                double[] series = _trainedSeries.Select(x => (double)x).ToArray();

                // Perform SSA decomposition
                DecomposeTimeSeries(series);

                Logger.LogInfo("SSA model training completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training SSA model", ex);
                return false;
            }
        }

        /// <summary>
        /// Predicts future sales based on recent data using the trained SSA model
        /// </summary>
        /// <param name="recentData">Recent sales data for making predictions</param>
        /// <param name="horizon">Number of periods to forecast into the future</param>
        /// <returns>List of forecasts with date, predicted value, and confidence intervals</returns>
        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> Predict(
            List<RossmannSalesRecord> recentData, 
            int horizon = 10)
        {
            try
            {
                Logger.LogInfo($"Generating SSA forecast for {horizon} periods ahead");
                
                if (_eigenvectors == null || _trainedSeries == null)
                {
                    Logger.LogWarning("SSA model not trained yet");
                    return new List<(DateTime, float, float, float)>();
                }

                // Get the most recent data points to use for prediction
                var recentSeries = recentData.Select(r => (double)r.Sales).ToList();
                if (recentSeries.Count < _windowSize)
                {
                    // If recent data is too short, pad with trained series data
                    recentSeries = _trainedSeries
                        .Skip(Math.Max(0, _trainedSeries.Count - (_windowSize - recentSeries.Count)))
                        .Select(x => (double)x)
                        .Concat(recentSeries)
                        .ToList();
                }

                // Use the last window_size values to forecast
                var lastWindow = recentSeries.Skip(Math.Max(0, recentSeries.Count - _windowSize)).Take(_windowSize).ToArray();
                
                // Calculate the forecast
                var forecasts = new List<(DateTime, float, float, float)>();
                DateTime lastDate = recentData.Last().Date;
                
                // Generate forecasts one step at a time
                var forecastSeries = new List<double>(lastWindow);
                for (int i = 0; i < horizon; i++)
                {
                    // Get the last window of data
                    var window = forecastSeries.Skip(forecastSeries.Count - _windowSize).Take(_windowSize).ToArray();
                    
                    // Make one-step forecast
                    double forecast = ForecastOneStep(window);
                    
                    // Add to forecasted series
                    forecastSeries.Add(forecast);
                    
                    // Calculate confidence intervals (assuming normal distribution with historical variance)
                    double stdDev = CalculateStandardDeviation(_trainedSeries.Select(x => (double)x).ToArray(), (float)forecast);
                    double margin = 1.96 * stdDev; // 95% confidence interval
                    
                    // Add to results with date
                    DateTime forecastDate = lastDate.AddDays(i + 1);
                    forecasts.Add((
                        forecastDate,
                        (float)forecast,
                        (float)Math.Max(0, forecast - margin), // Lower bound (can't be negative)
                        (float)(forecast + margin)              // Upper bound
                    ));
                }

                Logger.LogInfo($"SSA forecast complete with {forecasts.Count} periods");
                return forecasts;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating SSA forecast", ex);
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
                if (_eigenvectors == null || testData == null || testData.Count < 2)
                {
                    return float.NaN;
                }

                // Use sliding window evaluation
                float totalError = 0;
                int validPredictions = 0;

                for (int i = _windowSize; i < testData.Count; i++)
                {
                    var window = testData.Skip(i - _windowSize).Take(_windowSize).Select(r => (double)r.Sales).ToArray();
                    double actual = testData[i].Sales;
                    double predicted = ForecastOneStep(window);

                    // Calculate absolute percentage error
                    if (actual > 0)
                    {
                        float error = (float)Math.Abs((predicted - actual) / actual);
                        totalError += error;
                        validPredictions++;
                    }
                }

                // Return Mean Absolute Percentage Error
                return validPredictions > 0 ? (totalError / validPredictions) : float.NaN;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error evaluating SSA model", ex);
                return float.NaN;
            }
        }

        /// <summary>
        /// Decomposes the time series using SSA
        /// </summary>
        /// <param name="series">Time series to decompose</param>
        private void DecomposeTimeSeries(double[] series)
        {
            // Step 1: Embedding - create trajectory matrix
            int n = series.Length;
            int k = n - _windowSize + 1;
            
            var trajectoryMatrix = new double[k, _windowSize];
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < _windowSize; j++)
                {
                    trajectoryMatrix[i, j] = series[i + j];
                }
            }

            // Step 2: Calculate and center the covariance matrix
            _means = new double[_windowSize];
            for (int j = 0; j < _windowSize; j++)
            {
                double sum = 0;
                for (int i = 0; i < k; i++)
                {
                    sum += trajectoryMatrix[i, j];
                }
                _means[j] = sum / k;
            }

            // Center the trajectory matrix
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < _windowSize; j++)
                {
                    trajectoryMatrix[i, j] -= _means[j];
                }
            }

            // Step 3: Compute the SVD using a simplified approach
            // In a real implementation, you would use a proper SVD algorithm
            // Here we use a basic power iteration method to compute eigenvectors
            _eigenvectors = new List<double[]>();
            
            // Compute covariance matrix C = X'X
            var covMatrix = new double[_windowSize, _windowSize];
            for (int i = 0; i < _windowSize; i++)
            {
                for (int j = 0; j < _windowSize; j++)
                {
                    double sum = 0;
                    for (int m = 0; m < k; m++)
                    {
                        sum += trajectoryMatrix[m, i] * trajectoryMatrix[m, j];
                    }
                    covMatrix[i, j] = sum / (k - 1);
                }
            }

            // Extract top eigenvectors using power iteration
            for (int c = 0; c < Math.Min(_numComponents, _windowSize); c++)
            {
                var eigenvector = PowerIteration(covMatrix, 100, 1e-7);
                _eigenvectors.Add(eigenvector);
                
                // Deflate the matrix to find the next eigenvector
                DeflateMatrix(covMatrix, eigenvector);
            }
        }

        /// <summary>
        /// Simplified power iteration method to find the dominant eigenvector
        /// </summary>
        private double[] PowerIteration(double[,] matrix, int maxIterations, double tolerance)
        {
            int n = matrix.GetLength(0);
            double[] vector = new double[n];
            
            // Initialize with random unit vector
            var random = new Random(42);
            double norm = 0;
            for (int i = 0; i < n; i++)
            {
                vector[i] = random.NextDouble() - 0.5;
                norm += vector[i] * vector[i];
            }
            norm = Math.Sqrt(norm);
            for (int i = 0; i < n; i++)
            {
                vector[i] /= norm;
            }

            // Perform power iteration
            for (int iter = 0; iter < maxIterations; iter++)
            {
                double[] newVector = new double[n];
                
                // v' = A*v
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        newVector[i] += matrix[i, j] * vector[j];
                    }
                }

                // Normalize
                norm = 0;
                for (int i = 0; i < n; i++)
                {
                    norm += newVector[i] * newVector[i];
                }
                norm = Math.Sqrt(norm);
                
                // Check for convergence
                double diff = 0;
                for (int i = 0; i < n; i++)
                {
                    double normalized = newVector[i] / norm;
                    diff += Math.Abs(normalized - vector[i]);
                    vector[i] = normalized;
                }

                if (diff < tolerance)
                {
                    break;
                }
            }

            return vector;
        }

        /// <summary>
        /// Deflates a matrix by removing the component in the direction of the eigenvector
        /// </summary>
        private void DeflateMatrix(double[,] matrix, double[] eigenvector)
        {
            int n = matrix.GetLength(0);

            // Find eigenvalue (using Rayleigh quotient)
            double eigenvalue = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    sum += matrix[i, j] * eigenvector[j];
                }
                eigenvalue += eigenvector[i] * sum;
            }

            // Subtract v * v^T * λ from A
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] -= eigenvalue * eigenvector[i] * eigenvector[j];
                }
            }
        }

        /// <summary>
        /// Forecasts one step ahead based on a window of recent values
        /// </summary>
        /// <param name="window">Recent window of values to use for forecasting</param>
        /// <returns>Forecasted value</returns>
        private double ForecastOneStep(double[] window)
        {
            // Project the window onto each eigenvector to get the principal components
            var components = new double[Math.Min(_numComponents, _eigenvectors.Count)];

            for (int i = 0; i < components.Length; i++)
            {
                var eigenvector = _eigenvectors[i];
                double sum = 0;
                for (int j = 0; j < window.Length; j++)
                {
                    sum += (window[j] - _means[j]) * eigenvector[j];
                }
                components[i] = sum;
            }

            // Reconstruct the final value of the window
            double forecast = _means[_windowSize - 1]; // Start with the mean
            for (int i = 0; i < components.Length; i++)
            {
                forecast += components[i] * _eigenvectors[i][_windowSize - 1];
            }

            return forecast;
        }

        /// <summary>
        /// Calculates the standard deviation of the forecast error
        /// </summary>
        /// <param name="series">Historical series</param>
        /// <param name="forecast">Current forecast value</param>
        /// <returns>Standard deviation estimate</returns>
        private double CalculateStandardDeviation(double[] series, float forecast)
        {
            // If the series is short, use a simple heuristic
            if (series.Length < 30)
            {
                // Use a percentage of the forecast value as the standard deviation
                return Math.Max(1.0, forecast * 0.1);
            }

            // Calculate variations in the historical data
            double sum = 0;
            double sumSquared = 0;
            
            for (int i = 0; i < series.Length; i++)
            {
                sum += series[i];
                sumSquared += series[i] * series[i];
            }
            
            double mean = sum / series.Length;
            double variance = sumSquared / series.Length - mean * mean;
            
            // Return the standard deviation, with a minimum value
            return Math.Max(1.0, Math.Sqrt(variance));
        }
    }
}