using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using LiveCharts;
using LiveCharts.Wpf;

namespace DemandForecastingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Data.DataImporter dataImporter;
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
                    var records = dataImporter.ImportCsv(filename);
                    var cleanedRecords = dataImporter.CleanData(records);
                    
                    // Display basic statistics
                    dataImporter.PerformEDA(cleanedRecords);
                    
                    // Update status
                    StatusLabel.Content = $"Loaded {cleanedRecords.Count} records from {System.IO.Path.GetFileName(filename)}";
                }
                catch (Exception ex) {
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

            if (dataImporter.Records == null || dataImporter.Records.Count == 0)
            {
                MessageBox.Show("No data loaded. Please load data first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else {
                // Simulate forecast data (dummy data). Replace this with your ML model forecast logic.
                var forecastValues = new ChartValues<double> { 10, 12, 15, 13, 16, 18, 17, 19, 22, 20 };

                // Create a new series for the forecast data.
                var seriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Forecast",
                        Values = forecastValues
                    }
                };

                // Update the LiveCharts CartesianChart with the new data.
                ForecastChart.Series = seriesCollection;

                // Configure the X-Axis for time periods.
                ForecastChart.AxisX.Clear();
                ForecastChart.AxisX.Add(new Axis
                {
                    Title = "Time Period",
                    Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct" }
                });

                // Configure the Y-Axis for demand.
                ForecastChart.AxisY.Clear();
                ForecastChart.AxisY.Add(new Axis
                {
                    Title = "Demand",
                    LabelFormatter = value => value.ToString("N")
                });

                MessageBox.Show($"Forecast run with Lead Time: {leadTime} and Reorder Threshold: {reorderThreshold}");

                // Update the forecast details grid with the generated data
                UpdateForecastDetails(leadTime, reorderThreshold);
            }

        }
        
        // Helper method to update forecast details
        private void UpdateForecastDetails(int leadTime, int reorderThreshold)
        {
            // Create forecast data for the details grid
            var forecastDetails = new System.Collections.ObjectModel.ObservableCollection<ForecastDataPoint>();
            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct" };
            double[] demandValues = { 10, 12, 15, 13, 16, 18, 17, 19, 22, 20 };
            
            for (int i = 0; i < months.Length; i++)
            {
                forecastDetails.Add(new ForecastDataPoint
                {
                    Period = months[i],
                    ForecastedDemand = demandValues[i],
                    LowerBound = demandValues[i] * 0.9,
                    UpperBound = demandValues[i] * 1.1,
                    ReorderPoint = demandValues[i] < reorderThreshold ? "Yes" : "No"
                });
            }
            
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
            
            // Also generate inventory recommendations
            UpdateInventoryRecommendations(leadTime, reorderThreshold);
        }
        
        // Helper method to update inventory recommendations
        private void UpdateInventoryRecommendations(int leadTime, int reorderThreshold)
        {
            // Create sample inventory recommendations
            var recommendations = new System.Collections.ObjectModel.ObservableCollection<InventoryRecommendation>();
            
            // Generate sample data - in a real app this would come from your inventory optimization algorithm
            recommendations.Add(new InventoryRecommendation { Item = "Product A", CurrentStock = 8, RecommendedOrder = 15 });
            recommendations.Add(new InventoryRecommendation { Item = "Product B", CurrentStock = 12, RecommendedOrder = 10 });
            recommendations.Add(new InventoryRecommendation { Item = "Product C", CurrentStock = 5, RecommendedOrder = 20 });
            
            // Get the InventoryRecommendationsControl and update its data source
            var tabControl = FindName("MainTabControl") as TabControl;
            if (tabControl != null)
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
    }
    
    // Data classes for forecast and inventory
    public class ForecastDataPoint
    {
        public string Period { get; set; }
        public double ForecastedDemand { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public string ReorderPoint { get; set; }
    }
    
    public class InventoryRecommendation
    {
        public string Item { get; set; }
        public int CurrentStock { get; set; }
        public int RecommendedOrder { get; set; }
    }
}
