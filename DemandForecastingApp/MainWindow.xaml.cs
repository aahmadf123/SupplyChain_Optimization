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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TabControl tabControl)
            {
                // Handle tab selection change
                var selectedTab = tabControl.SelectedItem as TabItem;
                if (selectedTab != null)
                {
                    _viewModel.HandleTabChange(selectedTab.Header.ToString());
                }
            }
        }
    }
}
