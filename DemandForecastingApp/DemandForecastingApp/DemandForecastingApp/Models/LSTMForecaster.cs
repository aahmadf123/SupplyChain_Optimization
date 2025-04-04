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
        }

        public void SaveModel(string modelName)
        {
            try
            {
                if (_model == null)
                {
                    throw new InvalidOperationException("No model to save - train the model first.");
                }

                string modelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MODEL_FOLDER, modelName);
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
                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, 
                    new JsonSerializerOptions { WriteIndented = true }));

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
                string modelDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MODEL_FOLDER, modelName);
                string modelPath = Path.Combine(modelDir, "model.h5");
                string metadataPath = Path.Combine(modelDir, "metadata.json");

                if (!File.Exists(modelPath) || !File.Exists(metadataPath))
                {
                    Logger.LogWarning($"Model files not found in {modelDir}");
                    return false;
                }

                var metadata = JsonSerializer.Deserialize<ModelMetadata>(File.ReadAllText(metadataPath));
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

        public void Train(List<RossmannSalesRecord> trainingData, int epochs = 20, int batchSize = 32)
        {
            // Convert the training data to feature and label arrays
            int numSamples = trainingData.Count;
            _numFeatures = 9; // Update based on actual features used
            
            // Extract features from sales records (example features - adjust based on actual model)
            float[][] features = new float[numSamples][];
            float[] labels = new float[numSamples];
            
            for (int i = 0; i < numSamples; i++)
            {
                var record = trainingData[i];
                features[i] = new float[]
                {
                    record.StoreId,
                    record.Open,
                    record.Promo,
                    (float)record.DayOfWeek,
                    record.IsWeekend ? 1 : 0,
                    record.SchoolHoliday,
                    record.Month,
                    record.IsPublicHoliday ? 1 : 0,
                    record.Promo2
                };
                labels[i] = record.Sales;
            }
            
            // Normalize the data
            NormalizeData(features, labels);
            
            // Create time series data
            var (X, y) = CreateTimeSeriesData(features, labels);
            
            // Build and train the LSTM model
            BuildModel();
            
            // Convert to tensors for Keras
            var x_tensor = np.array(X);
            var y_tensor = np.array(y);
            
            // Train the model
            _model.fit(
                x_tensor, y_tensor,
                batch_size: batchSize,
                epochs: epochs,
                verbose: 1
            );
            
            Logger.LogInfo($"LSTM model trained on {numSamples} samples");
        }

        private void BuildModel()
        {
            // Create the LSTM model with appropriate layers
            _model = new Sequential();
            
            // Add LSTM layers
            _model.add(new LSTM(50, activation: "relu", return_sequences: true, 
                input_shape: new Shape(_timeSteps, _numFeatures)));
            _model.add(new LSTM(50, activation: "relu"));
            
            // Add Dense layers
            _model.add(new Dense(25, activation: "relu"));
            _model.add(new Dense(1)); // Output layer
            
            // Compile the model
            _model.compile(
                optimizer: new Adam(0.001f),
                loss: "mse"
            );
            
            Logger.LogInfo("LSTM model built successfully");
        }

        public List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> 
            PredictSales(List<RossmannSalesRecord> testData, int horizonDays)
        {
            if (_model == null)
            {
                throw new InvalidOperationException("Model not trained or loaded. Please train or load a model first.");
            }
            
            // Prepare test data for forecasting
            int numSamples = testData.Count;
            float[][] features = new float[numSamples][];
            
            for (int i = 0; i < numSamples; i++)
            {
                var record = testData[i];
                features[i] = new float[]
                {
                    record.StoreId,
                    record.Open,
                    record.Promo,
                    (float)record.DayOfWeek,
                    record.IsWeekend ? 1 : 0,
                    record.SchoolHoliday,
                    record.Month,
                    record.IsPublicHoliday ? 1 : 0,
                    record.Promo2
                };
            }
            
            // Normalize features
            for (int i = 0; i < numSamples; i++)
            {
                for (int j = 0; j < _numFeatures; j++)
                {
                    features[i][j] = (features[i][j] - _featureMeans[j]) / _featureStds[j];
                }
            }
            
            // Create sequences for prediction
            List<float[][]> sequences = new List<float[][]>();
            for (int i = 0; i <= numSamples - _timeSteps; i++)
            {
                float[][] seq = new float[_timeSteps][];
                for (int j = 0; j < _timeSteps; j++)
                {
                    seq[j] = features[i + j];
                }
                sequences.Add(seq);
            }
            
            // Convert to tensor
            var x_pred = np.array(sequences.ToArray());
            
            // Make predictions
            var predictions = _model.predict(x_pred);
            
            // Denormalize predictions and calculate confidence intervals
            var result = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
            
            float predictionStd = 0.1f; // Placeholder - adjust based on model performance
            
            for (int i = 0; i < predictions.shape[0]; i++)
            {
                if (i + _timeSteps < testData.Count)
                {
                    float prediction = predictions[i, 0] * _labelStd + _labelMean;
                    float lowerBound = prediction * (1 - predictionStd);
                    float upperBound = prediction * (1 + predictionStd);
                    
                    result.Add((testData[i + _timeSteps].Date, prediction, lowerBound, upperB
