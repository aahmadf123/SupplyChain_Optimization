using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.UI
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : System.Windows.Controls.UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            try
            {
                // Load API key from AppSettings
                txtApiKey.Text = AppSettings.GetSetting("AlphaVantageApiKey", "APFCJIALTDC7YYUT");
                
                // Load forecast horizon
                ForecastHorizonTextBox.Text = AppSettings.GetSetting("ForecastHorizon", "12");
                
                // Load ML model parameter
                MLModelParameterTextBox.Text = AppSettings.GetSetting("MLModelParameter", "Default");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");
                
                // Set defaults if loading fails
                txtApiKey.Text = "APFCJIALTDC7YYUT";
                ForecastHorizonTextBox.Text = "12";
                MLModelParameterTextBox.Text = "Default";
            }
        }
        
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate forecast horizon
                if (!int.TryParse(ForecastHorizonTextBox.Text, out int forecastHorizon))
                {
                    MessageBox.Show("Please enter a valid number for Forecast Horizon.");
                    return;
                }
                
                string mlModelParameter = MLModelParameterTextBox.Text;
                
                // Save settings to AppSettings
                AppSettings.SaveSetting("ForecastHorizon", forecastHorizon.ToString());
                AppSettings.SaveSetting("MLModelParameter", mlModelParameter);
                AppSettings.SaveSetting("AlphaVantageApiKey", txtApiKey.Text);
                
                MessageBox.Show("Settings saved successfully. Changes will take effect the next time you fetch market data.",
                               "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }
    }
}
