using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using DemandForecastingApp.Utils;
using System.Windows.Input;

namespace DemandForecastingApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _apiKey;
        private string _apiSecret;
        private string _apiEndpoint;
        private int _forecastHorizon;
        private float _confidenceLevel;
        private bool _useSeasonality;
        private int _seasonalityPeriod;
        private float _safetyStockFactor;
        private int _reorderPointDays;
        private string _dataFolderPath;
        private bool _autoLoadData;
        private bool _autoSaveResults;

        public SettingsViewModel()
        {
            LoadSettings();
            
            SaveCommand = new RelayCommand(SaveSettings, _ => true);
            CancelCommand = new RelayCommand(Cancel, _ => true);
            ResetCommand = new RelayCommand(ResetToDefaults, _ => true);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetCommand { get; }

        public string ApiKey
        {
            get => _apiKey;
            set
            {
                _apiKey = value;
                OnPropertyChanged();
            }
        }

        public string ApiSecret
        {
            get => _apiSecret;
            set
            {
                _apiSecret = value;
                OnPropertyChanged();
            }
        }

        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set
            {
                _apiEndpoint = value;
                OnPropertyChanged();
            }
        }

        public int ForecastHorizon
        {
            get => _forecastHorizon;
            set
            {
                _forecastHorizon = value;
                OnPropertyChanged();
            }
        }

        public float ConfidenceLevel
        {
            get => _confidenceLevel;
            set
            {
                _confidenceLevel = value;
                OnPropertyChanged();
            }
        }

        public bool UseSeasonality
        {
            get => _useSeasonality;
            set
            {
                _useSeasonality = value;
                OnPropertyChanged();
            }
        }

        public int SeasonalityPeriod
        {
            get => _seasonalityPeriod;
            set
            {
                _seasonalityPeriod = value;
                OnPropertyChanged();
            }
        }

        public float SafetyStockFactor
        {
            get => _safetyStockFactor;
            set
            {
                _safetyStockFactor = value;
                OnPropertyChanged();
            }
        }

        public int ReorderPointDays
        {
            get => _reorderPointDays;
            set
            {
                _reorderPointDays = value;
                OnPropertyChanged();
            }
        }

        public string DataFolderPath
        {
            get => _dataFolderPath;
            set
            {
                _dataFolderPath = value;
                OnPropertyChanged();
            }
        }

        public bool AutoLoadData
        {
            get => _autoLoadData;
            set
            {
                _autoLoadData = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSaveResults
        {
            get => _autoSaveResults;
            set
            {
                _autoSaveResults = value;
                OnPropertyChanged();
            }
        }

        private void LoadSettings()
        {
            ApiKey = AppSettings.GetSetting("ApiKey", "");
            ApiSecret = AppSettings.GetSetting("ApiSecret", "");
            ApiEndpoint = AppSettings.GetSetting("ApiEndpoint", "https://api.example.com");
            ForecastHorizon = int.Parse(AppSettings.GetSetting("ForecastHorizon", "30"));
            ConfidenceLevel = float.Parse(AppSettings.GetSetting("ConfidenceLevel", "0.95"));
            UseSeasonality = bool.Parse(AppSettings.GetSetting("UseSeasonality", "true"));
            SeasonalityPeriod = int.Parse(AppSettings.GetSetting("SeasonalityPeriod", "7"));
            SafetyStockFactor = float.Parse(AppSettings.GetSetting("SafetyStockFactor", "1.5"));
            ReorderPointDays = int.Parse(AppSettings.GetSetting("ReorderPointDays", "7"));
            DataFolderPath = AppSettings.GetSetting("DataFolderPath", "");
            AutoLoadData = bool.Parse(AppSettings.GetSetting("AutoLoadData", "false"));
            AutoSaveResults = bool.Parse(AppSettings.GetSetting("AutoSaveResults", "true"));
        }

        private void SaveSettings(object parameter)
        {
            AppSettings.SaveSetting("ApiKey", ApiKey);
            AppSettings.SaveSetting("ApiSecret", ApiSecret);
            AppSettings.SaveSetting("ApiEndpoint", ApiEndpoint);
            AppSettings.SaveSetting("ForecastHorizon", ForecastHorizon.ToString());
            AppSettings.SaveSetting("ConfidenceLevel", ConfidenceLevel.ToString());
            AppSettings.SaveSetting("UseSeasonality", UseSeasonality.ToString());
            AppSettings.SaveSetting("SeasonalityPeriod", SeasonalityPeriod.ToString());
            AppSettings.SaveSetting("SafetyStockFactor", SafetyStockFactor.ToString());
            AppSettings.SaveSetting("ReorderPointDays", ReorderPointDays.ToString());
            AppSettings.SaveSetting("DataFolderPath", DataFolderPath);
            AppSettings.SaveSetting("AutoLoadData", AutoLoadData.ToString());
            AppSettings.SaveSetting("AutoSaveResults", AutoSaveResults.ToString());

            if (parameter is System.Windows.Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private void Cancel(object parameter)
        {
            if (parameter is System.Windows.Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private void ResetToDefaults(object parameter)
        {
            AppSettings.ResetToDefaults();
            LoadSettings();
        }
    }
} 