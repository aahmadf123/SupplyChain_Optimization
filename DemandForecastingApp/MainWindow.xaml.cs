using System;
using System.Windows;
using System.Windows.Controls; // Fix: Added to support controls like TextBox
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
        public MainWindow()
        {
            InitializeComponent();
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
                // TODO: Insert your CSV data loading and preprocessing logic here.
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
            // TODO: Replace the dummy data with actual forecast results from your ML model.
        }
    }
}
