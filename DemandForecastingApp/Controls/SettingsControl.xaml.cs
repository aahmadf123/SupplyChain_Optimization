using System;
using System.Windows;
using System.Windows.Controls;
using DemandForecastingApp.ViewModels;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Controls
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : System.Windows.Controls.UserControl
    {
        private SettingsViewModel _viewModel;

        public SettingsControl()
        {
            InitializeComponent();
            
            try
            {
                _viewModel = new SettingsViewModel();
                DataContext = _viewModel;
                
                // Load settings when control is initialized
                _viewModel.LoadSettings();
                
                Logger.LogInfo("Settings control initialized");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error initializing settings control", ex);
                MessageBox.Show($"Error loading settings: {ex.Message}", 
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.SaveSettingsCommand.CanExecute(null))
                {
                    _viewModel.SaveSettingsCommand.Execute(null);
                    MessageBox.Show("Settings saved successfully", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
                MessageBox.Show($"Error saving settings: {ex.Message}", 
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.ResetToDefaultCommand.CanExecute(null))
                {
                    _viewModel.ResetToDefaultCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error resetting settings", ex);
                MessageBox.Show($"Error resetting settings: {ex.Message}", 
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DataPath_ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select Data Directory",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };
                
                var result = dialog.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    _viewModel.DataPath = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error selecting data directory", ex);
                MessageBox.Show($"Error selecting directory: {ex.Message}", 
                    "Directory Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExportPath_ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select Export Directory",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };
                
                var result = dialog.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    _viewModel.ExportPath = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error selecting export directory", ex);
                MessageBox.Show($"Error selecting directory: {ex.Message}", 
                    "Directory Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LogSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.LogInfo($"Current settings: Data path={_viewModel.DataPath}, " +
                    $"Export path={_viewModel.ExportPath}, Days to forecast={_viewModel.DaysToForecast}, " +
                    $"Auto save={_viewModel.AutoSaveResults}, Theme={_viewModel.SelectedTheme}");
                    
                MessageBox.Show("Settings logged to application log file", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error logging settings", ex);
            }
        }
    }
}