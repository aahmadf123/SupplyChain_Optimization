using System;
using System.Collections.Generic;
using System.Linq;
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
        private Sequential _model;
        private float[] _featureMeans;
        private float[] _featureStds;
        private float _labelMean;
        private float _labelStd;
        private int _timeSteps;
        private int _numFeatures;
        
        public LSTMForecaster(int timeSteps = 10)
        {
            _timeSteps = timeSteps;
        }
        
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
                var (X, y) = CreateTimeSeriesWindows(features, labels);
                
                // Create and compile model
                _numFeatures = features[0].Length;
                BuildModel();
                
                // Train the model
                _model.Fit(
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
            
            // Input layer
            _model.Add(new LSTM(50, return_sequences: true, 
                input_shape: new Shape(_timeSteps, _numFeatures)));
            
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
                
                // If we don't have enough data for the time steps, return empty
                if (testData.Count < _timeSteps)
                {
                    Logger.LogWarning($"Not enough data for prediction. Need at least {_timeSteps} records.");
                    return result;
                }
                
                // Get the last _timeSteps records for prediction
                var features = testData.TakeLast(_timeSteps).Select(r => r.ToFeatureVector()).ToArray();
                
                // Normalize features
                var normalizedFeatures = new float[features.Length][];
                for (int i = 0; i < features.Length; i++)
                {
                    normalizedFeatures[i] = new float[features[i].Length];
                    for (int j = 0; j < features[i].Length; j++)
                    {
                        normalizedFeatures[i][j] = (features[i][j] - _featureMeans[j]) / _featureStds[j];
                    }
                }
                
                // Reshape for LSTM input [samples, time_steps, features]
                var X = np.zeros(new Shape(1, _timeSteps, _numFeatures), TF_DataType.TF_FLOAT);
                for (int i = 0; i < _timeSteps; i++)
                {
                    for (int j = 0; j < _numFeatures; j++)
                    {
                        X[0, i, j] = normalizedFeatures[i][j];
                    }
                }
                
                // Get last date from the data
                var lastDate = testData.Last().Date;
                
                // Prediction for the next horizonDays
                for (int i = 0; i < horizonDays; i++)
                {
                    // Make prediction
                    var prediction = _model.Predict(X);
                    var forecastValue = (float)prediction[0, 0];
                    
                    // Denormalize the prediction
                    forecastValue = forecastValue * _labelStd + _labelMean;
                    
                    // Calculate uncertainty bounds (10% for simplicity)
                    float lowerBound = forecastValue * 0.9f;
                    float upperBound = forecastValue * 1.1f;
                    
                    // Next date to forecast
                    var forecastDate = lastDate.AddDays(i + 1);
                    
                    // Add to result
                    result.Add((forecastDate, forecastValue, lowerBound, upperBound));
                    
                    // Shift the window for the next day prediction
                    // In a real implementation, you would need to generate features for the next day
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
            float labelSum = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                labelSum += labels[i];
            }
            _labelMean = labelSum / labels.Length;
            
            float labelSumSquaredDiff = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                labelSumSquaredDiff += (float)Math.Pow(labels[i] - _labelMean, 2);
            }
            _labelStd = (float)Math.Sqrt(labelSumSquaredDiff / labels.Length);
            
            // Apply normalization to labels
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = (labels[i] - _labelMean) / _labelStd;
            }
        }
        
        private (NDArray X, NDArray y) CreateTimeSeriesWindows(float[][] features, float[] labels)
        {
            int samples = features.Length - _timeSteps;
            
            // Initialize arrays for the input (X) and output (y)
            var X = np.zeros(new Shape(samples, _timeSteps, features[0].Length), TF_DataType.TF_FLOAT);
            var y = np.zeros(new Shape(samples), TF_DataType.TF_FLOAT);
            
            // Create time windows
            for (int i = 0; i < samples; i++)
            {
                for (int j = 0; j < _timeSteps; j++)
                {
                    for (int k = 0; k < features[0].Length; k++)
                    {
                        X[i, j, k] = features[i + j][k];
                    }
                }
                y[i] = labels[i + _timeSteps];
            }
            
            return (X, y);
        }
    }
}