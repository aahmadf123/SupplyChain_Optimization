using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Handles serialization and deserialization of forecasting models
    /// </summary>
    public class ModelSerializer
    {
        private readonly string _modelDirectory;

        public ModelSerializer(string modelDirectory = null)
        {
            _modelDirectory = modelDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SupplyChainOptimization",
                "Models"
            );

            // Ensure directory exists
            Directory.CreateDirectory(_modelDirectory);
        }

        /// <summary>
        /// Saves a model to disk
        /// </summary>
        /// <param name="model">The model to save</param>
        /// <param name="modelName">Name of the model</param>
        /// <param name="version">Version of the model</param>
        /// <returns>Path to the saved model file</returns>
        public string SaveModel(IForecaster model, string modelName, string version = "1.0")
        {
            try
            {
                Logger.LogInfo($"Saving model {modelName} version {version}");

                var modelInfo = new ModelInfo
                {
                    ModelName = modelName,
                    Version = version,
                    Created = DateTime.UtcNow,
                    ModelType = model.GetType().Name,
                    Parameters = GetModelParameters(model)
                };

                string fileName = $"{modelName}_v{version}.json";
                string filePath = Path.Combine(_modelDirectory, fileName);

                // Serialize model info
                string json = JsonSerializer.Serialize(modelInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                Logger.LogInfo($"Model saved to {filePath}");

                return filePath;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving model {modelName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Loads a model from disk
        /// </summary>
        /// <param name="modelName">Name of the model to load</param>
        /// <param name="version">Version of the model to load</param>
        /// <returns>The loaded model info</returns>
        public ModelInfo LoadModel(string modelName, string version = "1.0")
        {
            try
            {
                Logger.LogInfo($"Loading model {modelName} version {version}");

                string fileName = $"{modelName}_v{version}.json";
                string filePath = Path.Combine(_modelDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Model file not found: {filePath}");
                }

                string json = File.ReadAllText(filePath);
                var modelInfo = JsonSerializer.Deserialize<ModelInfo>(json);

                Logger.LogInfo($"Model loaded from {filePath}");
                return modelInfo;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading model {modelName}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all available model versions
        /// </summary>
        /// <param name="modelName">Name of the model</param>
        /// <returns>List of available versions</returns>
        public List<string> GetAvailableVersions(string modelName)
        {
            try
            {
                var versions = new List<string>();
                string searchPattern = $"{modelName}_v*.json";
                string[] files = Directory.GetFiles(_modelDirectory, searchPattern);

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string version = fileName.Split("_v")[1];
                    versions.Add(version);
                }

                return versions;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting versions for model {modelName}", ex);
                return new List<string>();
            }
        }

        private Dictionary<string, object> GetModelParameters(IForecaster model)
        {
            var parameters = new Dictionary<string, object>();

            // Get model-specific parameters
            switch (model)
            {
                case SSAForecaster ssaModel:
                    parameters["WindowSize"] = ssaModel.WindowSize;
                    parameters["NumComponents"] = ssaModel.NumComponents;
                    break;

                case LSTMForecaster lstmModel:
                    parameters["LookbackWindow"] = lstmModel.LookbackWindow;
                    parameters["NumEpochs"] = lstmModel.NumEpochs;
                    break;
            }

            return parameters;
        }
    }

    /// <summary>
    /// Contains information about a saved model
    /// </summary>
    public class ModelInfo
    {
        public string ModelName { get; set; }
        public string Version { get; set; }
        public DateTime Created { get; set; }
        public string ModelType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        public override string ToString()
        {
            return $"Model: {ModelName} (v{Version})\n" +
                   $"Type: {ModelType}\n" +
                   $"Created: {Created:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Parameters: {string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))}";
        }
    }
} 