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
            set => SetProperty(ref _demandData, value);
        }
        
        private ObservableCollection<DemandRecord> _forecastData;
        public ObservableCollection<DemandRecord> ForecastData
        {
            get => _forecastData;
            set => SetProperty(ref _forecastData, value);
        }
        
        private string _selectedForecaster = "Standard";
        public string SelectedForecaster
        {
            get => _selectedForecaster;
            set => SetProperty(ref _selectedForecaster, value);
        }
        
        private float _leadTime = 7;
        public float LeadTime
        {
            get => _leadTime;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Lead time must be greater than 0");
                }
                SetProperty(ref _leadTime, value);
            }
        }
        
        private float _reorderPoint = 100;
        public float ReorderPoint
        {
            get => _reorderPoint;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Reorder point cannot be negative");
                }
                SetProperty(ref _reorderPoint, value);
            }
        }
        
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    // Make sure to update command availability when loading state changes
                    (LoadDataCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RunForecastCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ExportResultsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }
        
        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }
        
        // Available model types
        public ObservableCollection<string> AvailableModelTypes { get; } = new ObservableCollection<string> 
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
        private readonly IDataService _dataService;
        private readonly ML.ForecastModel _forecastModel;
        private List<RossmannSalesRecord> _salesData;
        
        public MainViewModel()
        {
            _dataService = new DataService();
            _forecastModel = new ML.ForecastModel();
            _demandData = new ObservableCollection<DemandRecord>();
            _forecastData = new ObservableCollection<DemandRecord>();
            _selectedForecaster = "Standard";
            _leadTime = 7;
            _reorderPoint = 100;
            _statusMessage = "Ready";
            
            // Initialize commands with proper CanExecute conditions
            LoadDataCommand = new RelayCommand(ExecuteLoadData, _ => !IsLoading);
            RunForecastCommand = new RelayCommand(ExecuteRunForecast, _ => !IsLoading && DemandData?.Count > 0);
            ExportResultsCommand = new RelayCommand(ExecuteExportResults, _ => !IsLoading && ForecastData?.Count > 0);
            GenerateDemoDataCommand = new RelayCommand(GenerateDemoData, _ => !IsLoading);
        }
        
        private async void ExecuteLoadData(object? parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";
                
                var data = await _dataService.LoadDemandDataAsync();
                DemandData.Clear();
                foreach (var record in data)
                {
                    DemandData.Add(record);
                }
                StatusMessage = $"Loaded {DemandData.Count} records";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading data", ex);
                StatusMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async void ExecuteRunForecast(object? parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Running forecast...";
                
                var forecast = await Task.Run(() => _forecastModel.PredictDemand(DemandData));
                ForecastData.Clear();
                foreach (var result in forecast)
                {
                    ForecastData.Add(result);
                }
                StatusMessage = "Forecast completed successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                StatusMessage = $"Error running forecast: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async void ExecuteExportResults(object? parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Exporting results...";
                
                await _dataService.ExportForecastResultsAsync(ForecastData);
                StatusMessage = "Results exported successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error exporting results", ex);
                StatusMessage = $"Error exporting results: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async void GenerateDemoData(object? parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Generating demo data...";
                
                if (!_dataService.VerifyRequiredDataFiles())
                {
                    if (!_dataService.CreateSampleDataFiles())
                    {
                        throw new Exception("Failed to create sample data files");
                    }
                }
                
                await Task.Run(() =>
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DemandData.Clear();
                        foreach (var record in demoData.Select(r => new DemandRecord
                        {
                            Date = r.Date,
                            Sales = r.Sales,
                            Store = r.Store.ToString()
                        }))
                        {
                            DemandData.Add(record);
                        }
                    });
                });

                StatusMessage = "Demo data generated successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating demo data", ex);
                MessageBox.Show($"Error generating demo data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error generating demo data";
            }
            finally
            {
                IsLoading = false;
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