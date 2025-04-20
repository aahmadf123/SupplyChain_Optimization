using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Data;
using DemandForecastingApp.ML;
using DemandForecastingApp.Utils;
using DemandForecastingApp.Services;

namespace DemandForecastingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Properties
        private ObservableCollection<DemandRecord> _demandData;
        public ObservableCollection<DemandRecord> DemandData
        {
            get => _demandData;
            set
            {
                _demandData = value;
                OnPropertyChanged();
            }
        }
        
        private ObservableCollection<ForecastPeriod> _forecastData;
        public ObservableCollection<ForecastPeriod> ForecastData
        {
            get => _forecastData;
            set
            {
                _forecastData = value;
                OnPropertyChanged();
            }
        }
        
        private string _selectedModelType = "Standard (Best for most situations)";
        public string SelectedModelType
        {
            get => _selectedModelType;
            set
            {
                _selectedModelType = value;
                OnPropertyChanged();
            }
        }
        
        private int _daysToForecast = 30;
        public int DaysToForecast
        {
            get => _daysToForecast;
            set
            {
                _daysToForecast = value;
                OnPropertyChanged();
            }
        }
        
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                // Make sure to update command availability when loading state changes
                (LoadDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RunForecastCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ExportResultsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }
        
        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }
        
        // Available model types
        public List<string> AvailableModelTypes { get; } = new List<string> 
        { 
            "Standard (Best for most situations)",
            "Advanced (For complex patterns)",
            "Simple (For basic trends)",
            "Seasonal (For products with seasonal demand)"
        };
        
        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand RunForecastCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand GenerateDemoDataCommand { get; }
        
        // Services and data
        private readonly DataImporter _dataImporter;
        private readonly Dictionary<string, IForecaster> _forecasters;
        private List<RossmannSalesRecord> _salesData;
        
        public MainViewModel()
        {
            // Initialize collections
            _demandData = new ObservableCollection<DemandRecord>();
            _forecastData = new ObservableCollection<ForecastPeriod>();
            
            // Initialize services
            _dataImporter = new DataImporter();
            _forecasters = new Dictionary<string, IForecaster>
            {
                { "Standard", new SSAForecaster() },
                { "Advanced", new LSTMForecaster() }
            };
            
            // Initialize commands
            LoadDataCommand = new RelayCommand(LoadData, CanLoadData);
            RunForecastCommand = new RelayCommand(RunForecast, CanRunForecast);
            ExportResultsCommand = new RelayCommand(ExportResults, CanExportResults);
            GenerateDemoDataCommand = new RelayCommand(GenerateDemoData);
        }
        
        private void LoadData(object parameter)
        {
            try
            {
                StatusMessage = "Loading data...";
                IsLoading = true;
                
                // Show file dialog to select data file
                var dialog = new OpenFileDialog
                {
                    Title = "Select Sales Data File",
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                
                if (dialog.ShowDialog() == true)
                {
                    string filePath = dialog.FileName;
                    
                    if (Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        // Use the regular DataImporter for CSV files
                        _demandData = new ObservableCollection<DemandRecord>(_dataImporter.ImportCsvData(filePath));
                        StatusMessage = $"Successfully loaded {_demandData.Count} records from {Path.GetFileName(filePath)}";
                    }
                    else if (IsRossmannDataFile(filePath))
                    {
                        // Use the RossmannDataImporter for specific Rossmann files
                        string dataFolder = Path.GetDirectoryName(filePath);
                        var rossmannImporter = new RossmannDataImporter();
                        _salesData = rossmannImporter.ImportData(dataFolder);
                        
                        // Convert Rossmann records to DemandRecords
                        _demandData = new ObservableCollection<DemandRecord>(
                            _salesData.Select(r => new DemandRecord
                            {
                                Date = r.Date,
                                Sales = r.Sales,
                                Store = r.Store.ToString()
                            }));
                        
                        StatusMessage = $"Successfully loaded {_demandData.Count} records from Rossmann dataset";
                    }
                    else
                    {
                        MessageBox.Show("Unsupported file format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusMessage = "Error: Unsupported file format";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading data", ex);
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error loading data";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanLoadData(object parameter)
        {
            return !IsLoading;
        }
        
        private async void RunForecast(object parameter)
        {
            try
            {
                if (_salesData == null || _salesData.Count == 0)
                {
                    MessageBox.Show("Please load data first", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = "Running forecast...";
                IsLoading = true;

                // Get the appropriate forecaster based on user selection
                string modelType = GetActualModelType(SelectedModelType);
                if (!_forecasters.TryGetValue(modelType, out var forecaster))
                {
                    MessageBox.Show("Invalid model type selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Train the model
                await Task.Run(() => forecaster.Train(_salesData));

                // Generate forecast
                var predictions = forecaster.Predict(_salesData, DaysToForecast);

                // Convert predictions to ForecastPeriods
                _forecastData = new ObservableCollection<ForecastPeriod>(
                    predictions.Select(p => new ForecastPeriod
                    {
                        Date = p.Date,
                        Forecast = p.Forecast,
                        LowerBound = p.LowerBound,
                        UpperBound = p.UpperBound
                    }));

                StatusMessage = "Forecast completed successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error running forecast";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private bool CanRunForecast(object parameter)
        {
            return !IsLoading && _salesData != null && _salesData.Count > 0;
        }
        
        private void ExportResults(object parameter)
        {
            try
            {
                if (_forecastData == null || _forecastData.Count == 0)
                {
                    MessageBox.Show("No forecast data to export", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Export Forecast Results",
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (dialog.ShowDialog() == true)
                {
                    using (var writer = new StreamWriter(dialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("Date,Forecast,LowerBound,UpperBound");

                        // Write data
                        foreach (var period in _forecastData)
                        {
                            writer.WriteLine($"{period.Date:yyyy-MM-dd},{period.Forecast},{period.LowerBound},{period.UpperBound}");
                        }
                    }

                    StatusMessage = $"Results exported to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error exporting results", ex);
                MessageBox.Show($"Error exporting results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error exporting results";
            }
        }
        
        private bool CanExportResults(object parameter)
        {
            return !IsLoading && _forecastData != null && _forecastData.Count > 0;
        }
        
        private void GenerateDemoData(object parameter)
        {
            try
            {
                var demoData = new List<RossmannSalesRecord>();
                var startDate = DateTime.Now.AddYears(-1);

                // Generate one year of demo data
                for (int i = 0; i < 365; i++)
                {
                    var date = startDate.AddDays(i);
                    var sales = 1000 + Math.Sin(i * 0.1) * 200 + (date.DayOfWeek == DayOfWeek.Saturday ? 300 : 0);
                    
                    demoData.Add(new RossmannSalesRecord
                    {
                        Date = date,
                        Store = 1,
                        Sales = (float)sales
                    });
                }

                _salesData = demoData;
                _demandData = new ObservableCollection<DemandRecord>(
                    demoData.Select(r => new DemandRecord
                    {
                        Date = r.Date,
                        Sales = r.Sales,
                        Store = r.Store.ToString()
                    }));

                StatusMessage = "Demo data generated successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating demo data", ex);
                MessageBox.Show($"Error generating demo data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error generating demo data";
            }
        }
        
        private string GetActualModelType(string userFriendlyName)
        {
            return userFriendlyName switch
            {
                "Standard (Best for most situations)" => "Standard",
                "Advanced (For complex patterns)" => "Advanced",
                "Simple (For basic trends)" => "Standard",
                "Seasonal (For products with seasonal demand)" => "Advanced",
                _ => "Standard"
            };
        }

        private bool IsRossmannDataFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath).ToLower();
            return fileName.Contains("rossmann") || fileName.Contains("store") || fileName.Contains("train");
        }
    }
}