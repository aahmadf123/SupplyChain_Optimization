using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DemandForecastingApp.Data;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace DemandForecastingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DataImporter _dataImporter;
        private List<Record> _loadedRecords;
        private SeriesCollection _forecastSeries;
        private string _leadTime;
        private string _reorderThreshold;
        private string _statusMessage;
        private ObservableCollection<ForecastDataPoint> _forecastPoints;
        private ObservableCollection<InventoryRecommendation> _inventoryRecommendations;
        private List<string> _labels;

        public ICommand LoadDataCommand { get; }
        public ICommand RunForecastCommand { get; }

        public string LeadTime
        {
            get => _leadTime;
            set => SetProperty(ref _leadTime, value);
        }

        public string ReorderThreshold
        {
            get => _reorderThreshold;
            set => SetProperty(ref _reorderThreshold, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SeriesCollection ForecastSeries
        {
            get => _forecastSeries;
            set => SetProperty(ref _forecastSeries, value);
        }

        public List<string> ChartLabels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }

        public ObservableCollection<ForecastDataPoint> ForecastPoints
        {
            get => _forecastPoints;
            set => SetProperty(ref _forecastPoints, value);
        }

        public ObservableCollection<InventoryRecommendation> InventoryRecommendations
        {
            get => _inventoryRecommendations;
            set => SetProperty(ref _inventoryRecommendations, value);
        }

        public Func<double, string> YFormatter { get; set; }

        public MainViewModel()
        {
            _dataImporter = new DataImporter();
            _forecastSeries = new SeriesCollection();
            _labels = new List<string>();
            _forecastPoints = new ObservableCollection<ForecastDataPoint>();
            _inventoryRecommendations = new ObservableCollection<InventoryRecommendation>();
            _statusMessage = "Ready";
            _leadTime = "3";
            _reorderThreshold = "100";

            // Format the chart for dark theme
            YFormatter = value => value.ToString("N1");

            LoadDataCommand = new RelayCommand(LoadData);
            RunForecastCommand = new RelayCommand(RunForecast, CanRunForecast);
        }

        private bool CanRunForecast(object parameter)
        {
            return !string.IsNullOrEmpty(LeadTime) && !string.IsNullOrEmpty(ReorderThreshold);
        }

        private void LoadData(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileName;
                
                try
                {
                    // Load the CSV data using the DataImporter
                    _loadedRecords = _dataImporter.ImportCsv(filename);
                    var cleanedRecords = _dataImporter.CleanData(_loadedRecords);
                    
                    // Store the cleaned records for later use
                    _loadedRecords = cleanedRecords;
                    
                    // Display basic statistics
                    _dataImporter.PerformEDA(cleanedRecords);
                    
                    // Update status
                    StatusMessage = $"Loaded {cleanedRecords.Count} records from {System.IO.Path.GetFileName(filename)}";
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error loading data", ex);
                    MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RunForecast(object parameter)
        {
            // Validate the input parameters for Lead Time and Reorder Threshold.
            if (!int.TryParse(LeadTime, out int leadTime))
            {
                MessageBox.Show("Invalid input for Lead Time. Please enter a number.");
                return;
            }
            if (!int.TryParse(ReorderThreshold, out int reorderThreshold))
            {
                MessageBox.Show("Invalid input for Reorder Threshold. Please enter a number.");
                return;
            }

            try
            {
                // For demo purposes, generate dummy forecast data
                var forecastResults = GenerateDemoForecastData();
                
                // Create chart values from forecast results
                var forecastValues = new ChartValues<double>();
                var lowerBoundValues = new ChartValues<double>();
                var upperBoundValues = new ChartValues<double>();
                var labels = new List<string>();
                
                foreach (var result in forecastResults)
                {
                    forecastValues.Add(result.Forecast);
                    lowerBoundValues.Add(result.LowerBound);
                    upperBoundValues.Add(result.UpperBound);
                    labels.Add(result.Date.ToString("MMM dd"));
                }
                
                // Create a new series collection for the chart
                var seriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Forecast",
                        Values = forecastValues
                    },
                    new LineSeries
                    {
                        Title = "Lower Bound",
                        Values = lowerBoundValues,
                        Stroke = System.Windows.Media.Brushes.LightBlue,
                        Fill = System.Windows.Media.Brushes.Transparent
                    },
                    new LineSeries
                    {
                        Title = "Upper Bound",
                        Values = upperBoundValues,
                        Stroke = System.Windows.Media.Brushes.LightBlue,
                        Fill = System.Windows.Media.Brushes.Transparent
                    }
                };
                
                // Update the chart
                ForecastSeries = seriesCollection;
                ChartLabels = labels;
                
                // Update forecast details and inventory recommendations
                UpdateForecastDetails(forecastResults, leadTime, reorderThreshold);
                
                // Log success
                Logger.LogInfo($"Forecast completed: Lead Time={leadTime}, Reorder Threshold={reorderThreshold}");
                
                StatusMessage = $"Forecast ran with Lead Time: {leadTime}, Reorder Threshold: {reorderThreshold}";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> GenerateDemoForecastData()
        {
            // Create demo forecast data
            var startDate = DateTime.Now;
            var results = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
            
            var baseValue = 100f;
            var random = new Random(123); // Fixed seed for reproducibility
            
            for (int i = 0; i < 10; i++)
            {
                var date = startDate.AddDays(i);
                var forecast = baseValue + (i * 5) + (float)(random.NextDouble() * 10 - 5);
                var lowerBound = forecast * 0.85f;
                var upperBound = forecast * 1.15f;
                
                results.Add((date, forecast, lowerBound, upperBound));
            }
            
            return results;
        }
        
        private void UpdateForecastDetails(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults, 
                                           int leadTime, int reorderThreshold)
        {
            // Create forecast data points
            var forecastDetails = new ObservableCollection<ForecastDataPoint>();
            
            foreach (var result in forecastResults)
            {
                forecastDetails.Add(new ForecastDataPoint
                {
                    Period = result.Date.ToString("MMM dd"),
                    Date = result.Date,
                    ForecastedDemand = result.Forecast,
                    LowerBound = result.LowerBound,
                    UpperBound = result.UpperBound,
                    ReorderPoint = result.Forecast < reorderThreshold ? "Yes" : "No"
                });
            }
            
            ForecastPoints = forecastDetails;
            
            // Update inventory recommendations
            UpdateInventoryRecommendations(forecastResults, leadTime, reorderThreshold);
        }
        
        private void UpdateInventoryRecommendations(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults,
                                                     int leadTime, int reorderThreshold)
        {
            var recommendations = new ObservableCollection<InventoryRecommendation>();
            
            // Generate demo inventory recommendations
            string[] products = { "A001", "B002", "C003" };
            
            foreach (var product in products)
            {
                // Calculate total forecasted demand during lead time
                double leadTimeDemand = forecastResults.Take(leadTime).Sum(r => r.Forecast);
                
                // Calculate safety stock based on variability
                double stdDev = CalculateStandardDeviation(forecastResults.Select(r => (double)r.Forecast));
                double safetyStock = stdDev * 1.65; // 95% service level
                
                // Calculate recommended order
                double currentStock = 10 + (product[0] - 'A') * 5; // Mock value based on product ID
                double recommendedOrder = Math.Max(0, leadTimeDemand + safetyStock - currentStock);
                
                recommendations.Add(new InventoryRecommendation
                {
                    Item = $"Product {product}",
                    CurrentStock = (int)currentStock,
                    RecommendedOrder = (int)Math.Ceiling(recommendedOrder),
                    LeadTimeDemand = Math.Round(leadTimeDemand, 2),
                    SafetyStock = Math.Round(safetyStock, 2)
                });
            }
            
            InventoryRecommendations = recommendations;
        }
        
        // Helper method to calculate standard deviation
        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();
            var avg = enumerable.Average();
            var sum = enumerable.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (enumerable.Length - 1));
        }
    }
}