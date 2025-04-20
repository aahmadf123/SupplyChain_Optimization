using System;
using System.Windows;
using System.Windows.Controls;
using DemandForecastingApp.Data;
using DemandForecastingApp.ViewModels;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            try
            {
                _viewModel = new MainViewModel();
                DataContext = _viewModel;
                
                Logger.LogInfo("Application started successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error initializing application", ex);
                MessageBox.Show($"Error initializing application: {ex.Message}", 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLoadData(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadData(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRunForecast(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.RunForecast(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportResults(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.ExportResults(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting results: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnGenerateDemoData(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.GenerateDemoData(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating demo data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                _viewModel.HandleTabChange(selectedTab.Header?.ToString());
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var helpWindow = new UI.HelpWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error opening help window", ex);
                MessageBox.Show($"Error opening help: {ex.Message}", 
                    "Help Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.LogInfo("Application exit requested");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during application shutdown", ex);
            }
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new UI.SettingsWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error opening settings window", ex);
                MessageBox.Show($"Error opening settings: {ex.Message}", 
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
