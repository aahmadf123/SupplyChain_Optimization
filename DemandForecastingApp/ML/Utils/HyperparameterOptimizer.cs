using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Implements hyperparameter optimization using grid search
    /// </summary>
    public class HyperparameterOptimizer
    {
        private readonly CrossValidator _crossValidator;
        private readonly int _numFolds;
        private readonly bool _parallelize;

        public HyperparameterOptimizer(int numFolds = 5, bool parallelize = true)
        {
            _numFolds = numFolds;
            _parallelize = parallelize;
            _crossValidator = new CrossValidator(numFolds);
        }

        /// <summary>
        /// Performs grid search to find optimal hyperparameters
        /// </summary>
        /// <param name="modelType">Type of model to optimize</param>
        /// <param name="data">Training data</param>
        /// <param name="parameterGrid">Grid of parameters to search</param>
        /// <returns>Best parameters and their performance</returns>
        public OptimizationResults OptimizeParameters(
            Type modelType,
            List<RossmannSalesRecord> data,
            Dictionary<string, object[]> parameterGrid)
        {
            try
            {
                Logger.LogInfo($"Starting hyperparameter optimization for {modelType.Name}");

                var results = new List<ParameterSetResult>();
                var parameterSets = GenerateParameterSets(parameterGrid);

                if (_parallelize)
                {
                    // Parallel processing for faster optimization
                    var tasks = parameterSets.Select(paramSet =>
                        Task.Run(() => EvaluateParameterSet(modelType, data, paramSet)));
                    
                    var completedTasks = Task.WhenAll(tasks).Result;
                    results.AddRange(completedTasks);
                }
                else
                {
                    // Sequential processing
                    foreach (var paramSet in parameterSets)
                    {
                        var result = EvaluateParameterSet(modelType, data, paramSet);
                        results.Add(result);
                    }
                }

                // Find best parameters
                var bestResult = results.OrderBy(r => r.CrossValidationResults.MeanMape).First();

                Logger.LogInfo($"Hyperparameter optimization completed. Best MAPE: {bestResult.CrossValidationResults.MeanMape:F4}");
                Logger.LogInfo($"Best parameters: {string.Join(", ", bestResult.Parameters.Select(p => $"{p.Key}={p.Value}"))}");

                return new OptimizationResults
                {
                    BestParameters = bestResult.Parameters,
                    BestPerformance = bestResult.CrossValidationResults.MeanMape,
                    AllResults = results
                };
            }
            catch (Exception ex)
            {
                Logger.LogError("Error in hyperparameter optimization", ex);
                throw;
            }
        }

        private List<Dictionary<string, object>> GenerateParameterSets(Dictionary<string, object[]> parameterGrid)
        {
            var parameterSets = new List<Dictionary<string, object>>();
            var keys = parameterGrid.Keys.ToList();
            
            GenerateParameterSetsRecursive(parameterGrid, keys, 0, new Dictionary<string, object>(), parameterSets);
            
            return parameterSets;
        }

        private void GenerateParameterSetsRecursive(
            Dictionary<string, object[]> parameterGrid,
            List<string> keys,
            int currentIndex,
            Dictionary<string, object> currentSet,
            List<Dictionary<string, object>> results)
        {
            if (currentIndex == keys.Count)
            {
                results.Add(new Dictionary<string, object>(currentSet));
                return;
            }

            string key = keys[currentIndex];
            foreach (var value in parameterGrid[key])
            {
                currentSet[key] = value;
                GenerateParameterSetsRecursive(parameterGrid, keys, currentIndex + 1, currentSet, results);
                currentSet.Remove(key);
            }
        }

        private ParameterSetResult EvaluateParameterSet(
            Type modelType,
            List<RossmannSalesRecord> data,
            Dictionary<string, object> parameters)
        {
            try
            {
                // Create model instance
                var model = (IForecaster)Activator.CreateInstance(modelType);

                // Apply parameters
                ApplyParameters(model, parameters);

                // Perform cross validation
                var cvResults = _crossValidator.ValidateModel(model, data);

                return new ParameterSetResult
                {
                    Parameters = parameters,
                    CrossValidationResults = cvResults
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error evaluating parameter set: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}", ex);
                return new ParameterSetResult
                {
                    Parameters = parameters,
                    CrossValidationResults = new CrossValidationResults { MeanMape = float.MaxValue }
                };
            }
        }

        private void ApplyParameters(IForecaster model, Dictionary<string, object> parameters)
        {
            switch (model)
            {
                case SSAForecaster ssaModel:
                    if (parameters.TryGetValue("WindowSize", out var windowSize))
                        ssaModel.WindowSize = Convert.ToInt32(windowSize);
                    if (parameters.TryGetValue("NumComponents", out var numComponents))
                        ssaModel.NumComponents = Convert.ToInt32(numComponents);
                    break;

                case LSTMForecaster lstmModel:
                    if (parameters.TryGetValue("LookbackWindow", out var lookbackWindow))
                        lstmModel.LookbackWindow = Convert.ToInt32(lookbackWindow);
                    if (parameters.TryGetValue("NumEpochs", out var numEpochs))
                        lstmModel.NumEpochs = Convert.ToInt32(numEpochs);
                    break;
            }
        }
    }

    /// <summary>
    /// Contains results for a single parameter set evaluation
    /// </summary>
    public class ParameterSetResult
    {
        public Dictionary<string, object> Parameters { get; set; }
        public CrossValidationResults CrossValidationResults { get; set; }

        public override string ToString()
        {
            return $"Parameters: {string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))}\n" +
                   $"Performance: {CrossValidationResults}";
        }
    }

    /// <summary>
    /// Contains the results of hyperparameter optimization
    /// </summary>
    public class OptimizationResults
    {
        public Dictionary<string, object> BestParameters { get; set; }
        public float BestPerformance { get; set; }
        public List<ParameterSetResult> AllResults { get; set; }

        public override string ToString()
        {
            return $"Best Parameters: {string.Join(", ", BestParameters.Select(p => $"{p.Key}={p.Value}"))}\n" +
                   $"Best Performance (MAPE): {BestPerformance:F4}\n" +
                   $"Total Parameter Sets Evaluated: {AllResults.Count}";
        }
    }
} 