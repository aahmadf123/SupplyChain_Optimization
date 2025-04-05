using System;
using System.Windows;
using System.Windows.Controls;
using DemandForecastingApp.Data;
using DemandForecastingApp.ViewModels;

namespace DemandForecastingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Set initial values for text boxes
            LeadTimeTextBox.Text = _viewModel.LeadTime;
            ReorderThresholdTextBox.Text = _viewModel.ReorderThreshold;
        }

        private void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use the ViewModel's command instead of direct loading
                if (_viewModel.LoadDataCommand.CanExecute(null))
                {
                    _viewModel.LoadDataCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a help window with guidance for users
            var helpWindow = new Window
            {
                Title = "Supply Chain Optimization Help",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = (System.Windows.Media.SolidColorBrush)Resources["BackgroundBrush"]
            };

            var scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(15),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var helpContent = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Add help content
            AddHelpSection(helpContent, "Getting Started",
                "1. Click 'Load Data' to import historical sales data\n" +
                "2. Set the Lead Time (days to receive inventory after ordering)\n" +
                "3. Set the Reorder Threshold (minimum inventory level that triggers a reorder)\n" +
                "4. Select a forecasting model (SSA or LSTM)\n" +
                "5. Click 'Run Forecast' to generate predictions and recommendations");

            AddHelpSection(helpContent, "Forecasting Models",
                "SSA (Default): Singular Spectrum Analysis - A traditional time series forecasting method that works well with seasonal data.\n\n" +
                "LSTM (Deep Learning): Long Short-Term Memory neural network - A deep learning approach that can capture complex patterns and relationships in the data. " +
                "LSTM typically performs better with large datasets and can incorporate multiple factors affecting demand.");

            AddHelpSection(helpContent, "Understanding Results",
                "Forecast Chart: Visualizes predicted demand over time with confidence intervals\n\n" +
                "Forecast Details: Tabular data showing forecasted values for each period\n\n" +
                "Inventory Recommendations: Suggested order quantities based on forecasted demand, lead time, and safety stock calculations\n\n" +
                "Market Analysis: Economic indicators and sector performance that may impact supply chain decisions");

            AddHelpSection(helpContent, "Tips for Supply Chain Students",
                "• Experiment with different lead times to see how they affect inventory recommendations\n" +
                "• Compare SSA and LSTM forecasts to understand the strengths of each approach\n" +
                "• Analyze how market indicators correlate with demand patterns\n" +
                "• Use the system to practice making inventory decisions in different scenarios");

            scrollViewer.Content = helpContent;
            helpWindow.Content = scrollViewer;
            helpWindow.ShowDialog();
        }

        private void AddHelpSection(StackPanel parent, string title, string content)
        {
            // Add section title
            parent.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.SolidColorBrush)Resources["TextBrush"],
                Margin = new Thickness(0, 10, 0, 5)
            });

            // Add section content
            parent.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (System.Windows.Media.SolidColorBrush)Resources["TextBrush"],
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Add separator
            parent.Children.Add(new Separator
            {
                Margin = new Thickness(0, 5, 0, 15),
                Background = (System.Windows.Media.SolidColorBrush)Resources["BorderBrush"]
            });
        }
    }
}
