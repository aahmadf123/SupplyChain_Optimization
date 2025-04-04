using System;
using System.Windows;
using DemandForecastingApp.Data;

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

        private void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load train, test, and store data
                var trainData = RossmannDataImporter.ImportSalesData("Data/train.csv");
                var testData = RossmannDataImporter.ImportTestData("Data/test.csv");
                var storeData = RossmannDataImporter.ImportStoreData("Data/store.csv");

                // Display the number of records loaded
                System.Windows.MessageBox.Show($"Loaded {trainData.Count} train records, {testData.Count} test records, and {storeData.Count} store records.",
                                "Data Loaded", MessageBoxButton.OK, MessageBoxImage.Information);

                // TODO: Bind the data to the UI or pass it to the forecasting logic
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
