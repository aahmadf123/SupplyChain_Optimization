using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using DemandForecastingApp.Utils;
using NumSharp;
using Tensorflow;
using Keras;
using Keras.Layers;
using Keras.Models;
using Keras.Optimizers;
using static Tensorflow.Binding;

namespace DemandForecastingApp.Models
{
    public class LSTMForecaster
    {
        private const string MODEL_VERSION = "1.0.0";
        private const string MODEL_FOLDER = "Models/Saved";
        private readonly string _baseModelPath;
        
        private Sequential? _model;
        private float[]? _featureMeans;
        private float[]? _featureStds;
        private float _labelMean;
        private float _labelStd;
        private int _timeSteps;
        private int _numFeatures;
        
        public bool IsModelLoaded => _model != null;
        
        public LSTMForecaster(int timeSteps = 10)
        {
            _timeSteps = timeSteps;
            _baseModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MODEL_FOLDER);
        }

        public void SaveModel(string modelName)
        {
            try
            {
                if (_model == null)
                {
                    throw new InvalidOperationException("No model to save - train the model first.");
                }

                string modelDir = Path.Combine(_baseModelPath, modelName);
                Directory.CreateDirectory(modelDir);

                string modelPath = Path.Combine(modelDir, "model.h5");
                _model.Save(modelPath);

                var metadata = new ModelMetadata
                {
                    Version = MODEL_VERSION,
                    TimeSteps = _timeSteps,
                    NumFeatures = _numFeatures,
                    FeatureMeans = _featureMeans,
                    FeatureStds = _featureStds,
                    LabelMean = _labelMean,
                    LabelStd = _labelStd,
                    SavedDate = DateTime.UtcNow
                };

                string metadataPath = Path.Combine(modelDir, "metadata.json");
                string jsonContent = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(metadataPath, jsonContent);

                Logger.LogInfo($"Model saved successfully to {modelDir}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving model: {ex.Message}", ex);
                throw;
            }
        }

        public bool LoadModel(string modelName)
        {
            try
            {
                string modelDir = Path.Combine(_baseModelPath, modelName);
                string modelPath = Path.Combine(modelDir, "model.h5");
                string metadataPath = Path.Combine(modelDir, "metadata.json");

                if (!File.Exists(modelPath) || !File.Exists(metadataPath))
                {
                    Logger.LogWarning($"Model files not found in {modelDir}");
                    return false;
                }

                string jsonContent = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<ModelMetadata>(jsonContent);
                
                if (metadata == null || metadata.Version != MODEL_VERSION)
                {
                    Logger.LogWarning($"Invalid or incompatible model version. Expected {MODEL_VERSION}, found {metadata?.Version}");
                    return false;
                }

                _timeSteps = metadata.TimeSteps;
                _numFeatures = metadata.NumFeatures;
                _featureMeans = metadata.FeatureMeans;
                _featureStds = metadata.FeatureStds;
                _labelMean = metadata.LabelMean;
                _labelStd = metadata.LabelStd;

                _model = Sequential.LoadModel(modelPath);

                Logger.LogInfo($"Model loaded successfully from {modelDir}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading model: {ex.Message}", ex);
                return false;
            }
        }

        public bool HasPreTrainedModel(string modelName)
        {
            try
            {
                string modelDir = Path.Combine(_baseModelPath, modelName);
                string modelPath = Path.Combine(modelDir, "model.h5");
                string metadataPath = Path.Combine(modelDir, "metadata.json");

                if (!File.Exists(modelPath) || !File.Exists(metadataPath))
                {
                    return false;
                }

                // Verify metadata is valid
                string jsonContent = File.ReadAllText(metadataPath);
                var metadata = JsonSerializer.Deserialize<ModelMetadata>(jsonContent);
                return metadata != null && metadata.Version == MODEL_VERSION;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking for pre-trained model: {ex.Message}", ex);
                return false;
            }
        }

        // Keep existing methods (no changes needed)
        public void Train(List<RossmannSalesRecord> trainingData, int epochs = 20, int batchSize = 32)
        {
            try
            {
                Logger.LogInfo("Starting LSTM model training");
                
                // Extract features and labels
                var features = trainingData.Select(r => r.ToFeatureVector()).ToArray();
                var labels = trainingData.Select(r => r.GetLabel()).ToArray();
                
                // Normalize data
                NormalizeData(features, labels);
                
                // Create time series windows
                var (X, y) = CreateTimeSeriesData(features, labels);
                
                // Create and compile model
                _numFeatures = features[0].Length;
                BuildModel();
                
                // Train the model
                _model?.Fit(
                    X, 
                    y,
                    batch_size: batchSize,
                    epochs: epochs,
                    validation_split: 0.2f
                );
                
                Logger.LogInfo("LSTM model training completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training LSTM model", ex);
                throw;
            }
        }

        private void BuildModel()
        {
            _model = new Sequential();
            
            // Input layer - use Keras.Shape explicitly
            _model.Add(new LSTM(50, return_sequences: true, 
                input_shape: new Keras.Shape(_timeSteps, _numFeatures)));
            
            // Hidden layers
            _model.Add(new Dropout(0.2f));
            _model.Add(new LSTM(50));
            _model.Add(new Dropout(0.2f));
            _model.Add(new Dense(25, activation: "relu"));
            
            // Output layer
            _model.Add(new Dense(1));
            
            // Compile the model
            _model.Compile(
                optimizer: new Adam(0.001f),
                loss: "mse",
                metrics: new[] { "mae" }
            );
        }

        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> 
            PredictSales(List<RossmannSalesRecord> testData, int horizonDays)
        {
            try
            {
                var result = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
                
                if (testData.Count < _timeSteps || _model == null)
                {
                    Logger.LogWarning($"Not enough data for prediction or model not trained. Need at least {_timeSteps} records.");
                    return result;
                }
                
                var features = testData.TakeLast(_timeSteps).Select(r => r.ToFeatureVector()).ToArray();
                
                var normalizedFeatures = new float[features.Length][];
                for (int i = 0; i < features.Length; i++)
                {
                    normalizedFeatures[i] = new float[features[i].Length];
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        normalizedFeatures[i][j] = (features[i][j] - _featureMeans![j]) / _featureStds![j];
                    }
                }
                
                float[][][] input = new float[1][][];
                input[0] = normalizedFeatures;
                
                var lastDate = testData.Last().Date;
                
                for (int i = 0; i < horizonDays; i++)
                {
                    float forecastValue = 0;
                    try
                    {
                        var prediction = _model.Predict(np.array(input));
                        forecastValue = prediction[0, 0] * _labelStd + _labelMean;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error making prediction: {ex.Message}", ex);
                        continue;
                    }
                    
                    float lowerBound = forecastValue * 0.9f;
                    float upperBound = forecastValue * 1.1f;
                    
                    var forecastDate = lastDate.AddDays(i + 1);
                    result.Add((forecastDate, forecastValue, lowerBound, upperBound));
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error predicting with LSTM model", ex);
                return new List<(DateTime, float, float, float)>();
            }
        }

        private void NormalizeData(float[][] features, float[] labels)
        {
            _featureMeans = new float[features[0].Length];
            _featureStds = new float[features[0].Length];
            
            // Calculate means and stds for features
            for (int j = 0; j < features[0].Length; j++)
            {
                float sum = 0;
                for (int i = 0; i < features.Length; i++)
                {
                    sum += features[i][j];
                }
                _featureMeans[j] = sum / features.Length;
                
                float sumSquaredDiff = 0;
                for (int i = 0; i < features.Length; i++)
                {
                    sumSquaredDiff += (float)Math.Pow(features[i][j] - _featureMeans[j], 2);
                }
                _featureStds[j] = (float)Math.Sqrt(sumSquaredDiff / features.Length);
                _featureStds[j] = _featureStds[j] == 0 ? 1 : _featureStds[j]; // Prevent division by zero
                
                // Apply normalization
                for (int i = 0; i < features.Length; i++)
                {
                    features[i][j] = (features[i][j] - _featureMeans[j]) / _featureStds[j];
                }
            }
            
            // Calculate mean and std for labels
            float labelSum = labels.Sum();
            _labelMean = labelSum / labels.Length;
            
            float labelSumSquaredDiff = labels.Sum(l => (float)Math.Pow(l - _labelMean, 2));
            _labelStd = (float)Math.Sqrt(labelSumSquaredDiff / labels.Length);
            
            // Apply normalization to labels
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = (labels[i] - _labelMean) / _labelStd;
            }
        }

        private (float[][], float[]) CreateTimeSeriesData(float[][] features, float[] labels)
        {
            int samples = features.Length - _timeSteps;
            
            float[][] X = new float[samples][];
            float[] y = new float[samples];
            
            for (int i = 0; i < samples; i++)
            {
                X[i] = new float[_timeSteps * features[0].Length];
                
                for (int j = 0; j < _timeSteps; j++)
                {
                    for (int k = 0; k < features[0].Length; k++)
                    {
                        X[i][j * features[0].Length + k] = features[i + j][k];
                    }
                }
                
                y[i] = labels[i + _timeSteps];
            }
            
            return (X, y);
        }
    }

    internal class ModelMetadata
    {
        public string Version { get; set; } = "";
        public int TimeSteps { get; set; }
        public int NumFeatures { get; set; }
        public float[]? FeatureMeans { get; set; }
        public float[]? FeatureStds { get; set; }
        public float LabelMean { get; set; }
        public float LabelStd { get; set; }
        public DateTime SavedDate { get; set; }
    }
}
