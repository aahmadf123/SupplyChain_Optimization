using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.IO;
using System.Configuration;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _alphaVantageApiKey;
        public string AlphaVantageApiKey
        {
            get => _alphaVantageApiKey;
            set
            {
                _alphaVantageApiKey = value;
                OnPropertyChanged();
            }
        }

        private string _weatherApiKey;
        public string WeatherApiKey
        {
            get => _weatherApiKey;
            set
            {
                _weatherApiKey = value;
                OnPropertyChanged();
            }
        }

        private int _defaultForecastHorizon;
        public int DefaultForecastHorizon
        {
            get => _defaultForecastHorizon;
            set
            {
                _defaultForecastHorizon = value;
                OnPropertyChanged();
            }
        }

        private double _confidenceInterval;
        public double ConfidenceInterval
        {
            get => _confidenceInterval;
            set
            {
                _confidenceInterval = value;
                OnPropertyChanged();
            }
        }

        private string _defaultModel;
        public string DefaultModel
        {
            get => _defaultModel;
            set
            {
                _defaultModel = value;
                OnPropertyChanged();
            }
        }

        private int _defaultLeadTime;
        public int DefaultLeadTime
        {
            get => _defaultLeadTime;
            set
            {
                _defaultLeadTime = value;
                OnPropertyChanged();
            }
        }

        private double _defaultReorderThreshold;
        public double DefaultReorderThreshold
        {
            get => _defaultReorderThreshold;
            set
            {
                _defaultReorderThreshold = value;
                OnPropertyChanged();
            }
        }

        private double _serviceLevel;
        public double ServiceLevel
        {
            get => _serviceLevel;
            set
            {
                _serviceLevel = value;
                OnPropertyChanged();
            }
        }

        private string _defaultDataDirectory;
        public string DefaultDataDirectory
        {
            get => _defaultDataDirectory;
            set
            {
                _defaultDataDirectory = value;
                OnPropertyChanged();
            }
        }

        private bool _autoSaveForecasts;
        public bool AutoSaveForecasts
        {
            get => _autoSaveForecasts;
            set
            {
                _autoSaveForecasts = value;
                OnPropertyChanged();
            }
        }

        private bool _autoRefreshMarketData;
        public bool AutoRefreshMarketData
        {
            get => _autoRefreshMarketData;
            set
            {
                _autoRefreshMarketData = value;
                OnPropertyChanged();
            }
        }

        public List<string> AvailableModels { get; } = new List<string> { "SSA", "LSTM" };

        public System.Windows.Input.ICommand BrowseDataDirectoryCommand { get; }
        public System.Windows.Input.ICommand SaveSettingsCommand { get; set; }
        public System.Windows.Input.ICommand CancelCommand { get; set; }

        public SettingsViewModel()
        {
            // Initialize commands
            BrowseDataDirectoryCommand = new System.Windows.Input.RelayCommand(_ => BrowseDataDirectory());

            // Load settings from configuration
            LoadSettings();
        }

        private void BrowseDataDirectory()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Default Data Directory";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    DefaultDataDirectory = dialog.SelectedPath;
                }
            }
        }

        public void LoadSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                AlphaVantageApiKey = config.AppSettings.Settings["AlphaVantageApiKey"]?.Value ?? "";
                WeatherApiKey = config.AppSettings.Settings["WeatherApiKey"]?.Value ?? "";
                DefaultForecastHorizon = int.Parse(config.AppSettings.Settings["DefaultForecastHorizon"]?.Value ?? "30");
                ConfidenceInterval = double.Parse(config.AppSettings.Settings["ConfidenceInterval"]?.Value ?? "95");
                DefaultModel = config.AppSettings.Settings["DefaultModel"]?.Value ?? "SSA";
                DefaultLeadTime = int.Parse(config.AppSettings.Settings["DefaultLeadTime"]?.Value ?? "7");
                DefaultReorderThreshold = double.Parse(config.AppSettings.Settings["DefaultReorderThreshold"]?.Value ?? "100");
                ServiceLevel = double.Parse(config.AppSettings.Settings["ServiceLevel"]?.Value ?? "95");
                DefaultDataDirectory = config.AppSettings.Settings["DefaultDataDirectory"]?.Value ?? 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SupplyChainData");
                AutoSaveForecasts = bool.Parse(config.AppSettings.Settings["AutoSaveForecasts"]?.Value ?? "true");
                AutoRefreshMarketData = bool.Parse(config.AppSettings.Settings["AutoRefreshMarketData"]?.Value ?? "true");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading settings", ex);
                // Use default values if loading fails
                SetDefaultValues();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                config.AppSettings.Settings["AlphaVantageApiKey"].Value = AlphaVantageApiKey;
                config.AppSettings.Settings["WeatherApiKey"].Value = WeatherApiKey;
                config.AppSettings.Settings["DefaultForecastHorizon"].Value = DefaultForecastHorizon.ToString();
                config.AppSettings.Settings["ConfidenceInterval"].Value = ConfidenceInterval.ToString();
                config.AppSettings.Settings["DefaultModel"].Value = DefaultModel;
                config.AppSettings.Settings["DefaultLeadTime"].Value = DefaultLeadTime.ToString();
                config.AppSettings.Settings["DefaultReorderThreshold"].Value = DefaultReorderThreshold.ToString();
                config.AppSettings.Settings["ServiceLevel"].Value = ServiceLevel.ToString();
                config.AppSettings.Settings["DefaultDataDirectory"].Value = DefaultDataDirectory;
                config.AppSettings.Settings["AutoSaveForecasts"].Value = AutoSaveForecasts.ToString();
                config.AppSettings.Settings["AutoRefreshMarketData"].Value = AutoRefreshMarketData.ToString();

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                Logger.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
                System.Windows.MessageBox.Show("Error saving settings: " + ex.Message, 
                    "Settings Error", System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void SetDefaultValues()
        {
            AlphaVantageApiKey = "";
            WeatherApiKey = "";
            DefaultForecastHorizon = 30;
            ConfidenceInterval = 95;
            DefaultModel = "SSA";
            DefaultLeadTime = 7;
            DefaultReorderThreshold = 100;
            ServiceLevel = 95;
            DefaultDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SupplyChainData");
            AutoSaveForecasts = true;
            AutoRefreshMarketData = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 