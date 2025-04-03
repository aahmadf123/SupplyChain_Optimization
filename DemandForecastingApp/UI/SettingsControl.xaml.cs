using System.Windows;
using System.Windows.Controls;

namespace DemandForecastingApp.UI
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
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
            
            // TODO: Save these settings, for example to a configuration file or settings object.
            MessageBox.Show($"Settings Saved:\nForecast Horizon: {forecastHorizon}\nML Model Parameter: {mlModelParameter}");
        }
    }
}
