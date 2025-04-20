using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Implements k-fold cross validation for model evaluation
    /// </summary>
    public class CrossValidator
    {
        private readonly Random _random = new Random(42);
        private readonly int _numFolds;
        private readonly bool _shuffleData;

        public CrossValidator(int numFolds = 5, bool shuffleData = true)
        {
            _numFolds = Math.Max(2, Math.Min(numFolds, 10)); // Constrain between 2 and 10 folds
            _shuffleData = shuffleData;
        }

        /// <summary>
        /// Performs k-fold cross validation on the specified model and data
        /// </summary>
        /// <param name="model">The forecasting model to validate</param>
        /// <param name="data">Historical sales data</param>
        /// <returns>Cross validation results including mean and standard deviation of MAPE</returns>
        public CrossValidationResults ValidateModel(IForecaster model, List<RossmannSalesRecord> data)
        {
            try
            {
                Logger.LogInfo($"Starting {_numFolds}-fold cross validation");

                if (data == null || data.Count < _numFolds * 2)
                {
                    throw new ArgumentException($"Insufficient data for {_numFolds}-fold cross validation");
                }

                // Prepare data folds
                var folds = PrepareFolds(data);
                var foldResults = new List<float>();

                // Perform validation on each fold
                for (int i = 0; i < _numFolds; i++)
                {
                    Logger.LogInfo($"Validating fold {i + 1}/{_numFolds}");

                    // Prepare training and validation data
                    var trainingData = new List<RossmannSalesRecord>();
                    var validationData = folds[i];

                    for (int j = 0; j < _numFolds; j++)
                    {
                        if (j != i)
                        {
                            trainingData.AddRange(folds[j]);
                        }
                    }

                    // Train model on training data
                    if (!model.Train(trainingData))
                    {
                        Logger.LogWarning($"Model training failed for fold {i + 1}");
                        continue;
                    }

                    // Evaluate on validation data
                    float foldMape = model.Evaluate(validationData);
                    if (!float.IsNaN(foldMape))
                    {
                        foldResults.Add(foldMape);
                    }
                }

                // Calculate statistics
                if (foldResults.Count == 0)
                {
                    throw new Exception("No valid results from cross validation");
                }

                float meanMape = foldResults.Average();
                float stdDevMape = CalculateStandardDeviation(foldResults);

                Logger.LogInfo($"Cross validation completed. Mean MAPE: {meanMape:F4}, StdDev: {stdDevMape:F4}");

                return new CrossValidationResults
                {
                    MeanMape = meanMape,
                    StandardDeviationMape = stdDevMape,
                    FoldResults = foldResults,
                    NumFolds = _numFolds
                };
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in cross validation", ex);
                throw;
            }
        }

        private List<List<RossmannSalesRecord>> PrepareFolds(List<RossmannSalesRecord> data)
        {
            var folds = new List<List<RossmannSalesRecord>>();
            var dataCopy = new List<RossmannSalesRecord>(data);

            if (_shuffleData)
            {
                // Fisher-Yates shuffle
                for (int i = dataCopy.Count - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    var temp = dataCopy[i];
                    dataCopy[i] = dataCopy[j];
                    dataCopy[j] = temp;
                }
            }

            // Create folds
            int foldSize = dataCopy.Count / _numFolds;
            int remainder = dataCopy.Count % _numFolds;

            int startIndex = 0;
            for (int i = 0; i < _numFolds; i++)
            {
                int currentFoldSize = foldSize + (i < remainder ? 1 : 0);
                folds.Add(dataCopy.GetRange(startIndex, currentFoldSize));
                startIndex += currentFoldSize;
            }

            return folds;
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
            return (float)Math.Sqrt(sumSquaredDiffs / (values.Count - 1));
        }
    }

    /// <summary>
    /// Contains the results of cross validation
    /// </summary>
    public class CrossValidationResults
    {
        public float MeanMape { get; set; }
        public float StandardDeviationMape { get; set; }
        public List<float> FoldResults { get; set; }
        public int NumFolds { get; set; }

        public override string ToString()
        {
            return $"Cross Validation Results ({NumFolds} folds):\n" +
                   $"Mean MAPE: {MeanMape:F4}\n" +
                   $"Standard Deviation MAPE: {StandardDeviationMape:F4}";
        }
    }
} 