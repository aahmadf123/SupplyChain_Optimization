using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DemandForecastingApp.Data;
using DemandForecastingApp.Models;
using DemandForecastingApp.Services;
using DemandForecastingApp.Utils;
        private async void LoadData(object? parameter)
        {
            try
            {
                string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                StatusMessage = "Loading Rossmann dataset from Data folder...";
                
                await Task.Run(() =>
                {
                    // Load Rossmann dataset directly from the Data folder
                    _rossmannRecords = _rossmannDataImporter.ImportData(dataFolderPath);
                    
                    // Perform feature engineering
                    _rossmannDataImporter.FeatureEngineering();
                    
                    _isRossmannDataLoaded = true;
                });
                
                StatusMessage = $"Loaded {_rossmannRecords?.Count ?? 0} Rossmann sales records";
                
                // If LSTM is selected, try to load pre-trained model first
                if (SelectedModelType.Contains("LSTM"))
                {
                    await InitializeLSTMModelAsync();
                }
                            _rossmannRecords = _rossmannDataImporter.ImportData(folderPath);
                            
                            // Perform feature engineering
                            _rossmannDataImporter.FeatureEngineering();
                            
                            _isRossmannDataLoaded = true;
                        });
                        
                        StatusMessage = $"Loaded {_rossmannRecords?.Count ?? 0} Rossmann sales records";
                        
                        // If LSTM is selected, try to load pre-trained model first
                        if (SelectedModelType.Contains("LSTM"))
                        {
                            await InitializeLSTMModelAsync();
                        }

        private async Task InitializeLSTMModelAsync()
        {
            try
            {
                _lstmForecaster = new LSTMForecaster();
                
                // Try to load pre-trained model first
                if (_lstmForecaster.HasPreTrainedModel("default"))
                {
                    StatusMessage = "Loading pre-trained LSTM model...";
                    if (_lstmForecaster.LoadModel("default"))
                    {
                        StatusMessage = "Pre-trained LSTM model loaded successfully";
                        return;
                    }
                }
                
                // If no pre-trained model is available or loading fails, train a new one
                await TrainLSTMModelAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error initializing LSTM model", ex);
                throw;
            }
        }

        private async Task TrainLSTMModelAsync()
        {
            try
            {
                StatusMessage = "Training LSTM model (this may take a while)...";
                
                await Task.Run(() =>
                {
                    if (_lstmForecaster == null)
                    {
                        _lstmForecaster = new LSTMForecaster();
                    }
                    
                    // Use a subset for training to keep it manageable
                    var trainingData = _rossmannRecords?
                        .Where(r => r.Sales.HasValue)
                        .Take(10000)  // Limit for demo purposes
                        .ToList() ?? new List<RossmannSalesRecord>();
                    
                    _lstmForecaster.Train(trainingData, epochs: 5);  // Reduced epochs for demo
                    
                    // Save the trained model
                    _lstmForecaster.SaveModel("default");
                });
                
                StatusMessage = "LSTM model training completed and saved";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training LSTM model", ex);
                MessageBox.Show($"Error training LSTM model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error training LSTM model";
            }
        }

