using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Win32;
using LiveCharts;
using LiveCharts.Wpf;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Data.DataImporter dataImporter;
        private List<Data.Record> loadedRecords;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize the chart with empty data
            ForecastChart.Series = new SeriesCollection();
            ForecastChart.AxisX.Add(new Axis { Title = "Time Period" });
            
            dataImporter = new Data.DataImporter();
            ForecastChart.AxisY.Add(new Axis { Title = "Demand", LabelFormatter = value => value.ToString("N") });
        }

        // Handler for the "Load Data" button click.
        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            // Open a file dialog to select a CSV file.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileName;
                MessageBox.Show($"Loaded data file: {filename}");
                
                try {
                    // Load the CSV data using the DataImporter
                    loadedRecords = dataImporter.ImportCsv(filename);
                    var cleanedRecords = dataImporter.CleanData(loadedRecords);
                    
                    // Store the cleaned records for later use
                    loadedRecords = cleanedRecords;
                    
                    // Display basic statistics
                    dataImporter.PerformEDA(cleanedRecords);
                    
                    // Update status
                    StatusLabel.Content = $"Loaded {cleanedRecords.Count} records from {System.IO.Path.GetFileName(filename)}";
                }
                catch (Exception ex) {
                    Logger.LogError("Error loading data", ex);
                    MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Handler for the "Run Forecast" button click.
        private void RunForecast_Click(object sender, RoutedEventArgs e)
        {
            // Validate the input parameters for Lead Time and Reorder Threshold.
            if (!int.TryParse(LeadTimeTextBox.Text, out int leadTime))
            {
                MessageBox.Show("Invalid input for Lead Time. Please enter a number.");
                return;
            }
            if (!int.TryParse(ReorderThresholdTextBox.Text, out int reorderThreshold))
            {
                MessageBox.Show("Invalid input for Reorder Threshold. Please enter a number.");
                return;
            }

            // Check if data is loaded
            if (loadedRecords == null || loadedRecords.Count == 0)
            {
                MessageBox.Show("No data loaded. Please load data first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Convert to DemandRecord for ML model
                var demandRecords = new List<DemandRecord>();
                foreach (var record in loadedRecords)
                {
                    demandRecords.Add(new DemandRecord
                    {
                        Date = record.Date,
                        Demand = (float)record.Demand
                    });
                }
                
                // Create and use the forecast model
                var forecastModel = new Models.ForecastModel();
                var forecastHorizon = 10; // Default to 10 periods
                var forecastResults = forecastModel.PredictDemand(demandRecords, forecastHorizon);
                
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
                ForecastChart.Series = seriesCollection;
                ForecastChart.AxisX.Clear();
                ForecastChart.AxisX.Add(new Axis
                {
                    Title = "Date",
                    Labels = labels
                });
                
                // Update forecast details in UI
                UpdateForecastDetails(forecastResults, leadTime, reorderThreshold);
                
                // Log success
                Logger.LogInfo($"Forecast completed: Lead Time={leadTime}, Reorder Threshold={reorderThreshold}");
                
                MessageBox.Show($"Forecast run with Lead Time: {leadTime} and Reorder Threshold: {reorderThreshold}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Helper method to update forecast details
        private void UpdateForecastDetails(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults, 
                                          int leadTime, int reorderThreshold)
        {
            // Create forecast data points for the details grid
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
            
            // Also generate inventory recommendations
            UpdateInventoryRecommendations(forecastResults, leadTime, reorderThreshold);
            
            // Get the ForecastDetailsControl from the tab control and update its data source
            var tabControl = FindName("MainTabControl") as TabControl;
            if (tabControl != null)
            {
                var forecastDetailsTab = tabControl.Items[0] as TabItem;
                if (forecastDetailsTab != null)
                {
                    var forecastDetailsControl = forecastDetailsTab.Content as UI.ForecastDetailsControl;
                    if (forecastDetailsControl != null)
                    {
                        forecastDetailsControl.UpdateForecastData(forecastDetails);
                    }
                }
            }
        }
        
        // Helper method to update inventory recommendations
        private void UpdateInventoryRecommendations(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults,
                                                   int leadTime, int reorderThreshold)
        {
            var recommendations = new ObservableCollection<InventoryRecommendation>();
            
            // Group records by product ID to generate recommendations for each product
            var products = loadedRecords.Select(r => r.ProductId).Distinct().Take(3).ToList(); // Limit to 3 products for demo
            
            foreach (var product in products)
            {
                // Calculate total forecasted demand during lead time
                double leadTimeDemand = forecastResults.Take(leadTime).Sum(r => r.Forecast);
                
                // Calculate safety stock based on variability
                double stdDev = CalculateStandardDeviation(forecastResults.Select(r => (double)r.Forecast));
                double safetyStock = stdDev * 1.65; // 95% service level
                
                // Calculate recommended order
                double currentStock = 10; // Mock value - in real app would come from inventory system
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
            
            // Get the InventoryRecommendationsControl and update its data source
            var tabControl = FindName("MainTabControl") as TabControl;
            if (tabControl != null && tabControl.Items.Count > 1)
            {
                var recommendationsTab = tabControl.Items[1] as TabItem;
                if (recommendationsTab != null)
                {
                    var recommendationsControl = recommendationsTab.Content as UI.InventoryRecommendationsControl;
                    if (recommendationsControl != null)
                    {
                        recommendationsControl.UpdateRecommendations(recommendations);
                    }
                }
            }
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
