using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.UI
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private readonly string _settingsFilePath = "settings.xml";
        
        public SettingsControl()
        {
            InitializeComponent();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve settings from text boxes.
            if (!int.TryParse(ForecastHorizonTextBox.Text, out int forecastHorizon))
            {
                MessageBox.Show("Please enter a valid number for Forecast Horizon.");
                return;
            }

            string mlModelParameter = MLModelParameterTextBox.Text;
            
            // Create settings object and save
            var settings = new AppSettingsData
            {
                ForecastHorizon = forecastHorizon,
                MLModelParameter = mlModelParameter,
                LastUpdated = DateTime.Now
            };
            
            try
            {
                // Serialize settings to XML
                var serializer = new XmlSerializer(typeof(AppSettingsData));
                using (var writer = new StreamWriter(_settingsFilePath))
                {
                    serializer.Serialize(writer, settings);
                }
                
                MessageBox.Show($"Settings Saved:\nForecast Horizon: {forecastHorizon}\nML Model Parameter: {mlModelParameter}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
            
            // Save API key
            Utils.AppSettings.SaveSetting("AlphaVantageApiKey", txtApiKey.Text);
            
            MessageBox.Show("Settings saved successfully. Changes will take effect the next time you fetch market data.",
                           "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var serializer = new XmlSerializer(typeof(AppSettingsData));
                    using (var reader = new StreamReader(_settingsFilePath))
                    {
                        var settings = (AppSettingsData)serializer.Deserialize(reader);
                        ForecastHorizonTextBox.Text = settings.ForecastHorizon.ToString();
                        MLModelParameterTextBox.Text = settings.MLModelParameter;
                    }
                }
                else
                {
                    // Set defaults
                    ForecastHorizonTextBox.Text = "12";
                    MLModelParameterTextBox.Text = "Default";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");
                // Set defaults if loading fails
                ForecastHorizonTextBox.Text = "12";
                MLModelParameterTextBox.Text = "Default";
            }
        }
    }
    
    // Settings class for serialization - renamed to avoid conflict
    [Serializable]
    public class AppSettingsData
    {
        public int ForecastHorizon { get; set; }
        public string MLModelParameter { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
