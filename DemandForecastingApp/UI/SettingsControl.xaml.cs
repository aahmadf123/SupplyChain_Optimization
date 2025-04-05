using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DemandForecastingApp.Utils;
using MessageBox = System.Windows.MessageBox;

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

                // Load forecasting settings
                ForecastHorizonTextBox.Text = AppSettings.GetSetting("ForecastHorizon", "10");
                ConfidenceLevelTextBox.Text = AppSettings.GetSetting("ConfidenceLevel", "95");
                SafetyStockFactorTextBox.Text = AppSettings.GetSetting("SafetyStockFactor", "1.65");

                // Load deep learning settings
                LSTMEpochsTextBox.Text = AppSettings.GetSetting("LSTMEpochs", "20");
                BatchSizeTextBox.Text = AppSettings.GetSetting("BatchSize", "32");
                LearningRateTextBox.Text = AppSettings.GetSetting("LearningRate", "0.001");
                LSTMUnitsTextBox.Text = AppSettings.GetSetting("LSTMUnits", "50");
                DropoutRateTextBox.Text = AppSettings.GetSetting("DropoutRate", "0.2");
                UseGPUCheckBox.IsChecked = bool.Parse(AppSettings.GetSetting("UseGPU", "false"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");

                // Set defaults if loading fails
                txtApiKey.Text = "APFCJIALTDC7YYUT";
                ForecastHorizonTextBox.Text = "10";
                ConfidenceLevelTextBox.Text = "95";
                SafetyStockFactorTextBox.Text = "1.65";
                LSTMEpochsTextBox.Text = "20";
                BatchSizeTextBox.Text = "32";
                LearningRateTextBox.Text = "0.001";
                LSTMUnitsTextBox.Text = "50";
                DropoutRateTextBox.Text = "0.2";
                UseGPUCheckBox.IsChecked = false;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate numeric inputs
                if (!ValidateNumericInput(ForecastHorizonTextBox.Text, "Forecast Horizon", true) ||
                    !ValidateNumericInput(ConfidenceLevelTextBox.Text, "Confidence Level", true) ||
                    !ValidateNumericInput(SafetyStockFactorTextBox.Text, "Safety Stock Factor", false) ||
                    !ValidateNumericInput(LSTMEpochsTextBox.Text, "LSTM Epochs", true) ||
                    !ValidateNumericInput(BatchSizeTextBox.Text, "Batch Size", true) ||
                    !ValidateNumericInput(LearningRateTextBox.Text, "Learning Rate", false) ||
                    !ValidateNumericInput(LSTMUnitsTextBox.Text, "LSTM Units", true) ||
                    !ValidateNumericInput(DropoutRateTextBox.Text, "Dropout Rate", false))
                {
                    return;
                }

                // Save API key
                AppSettings.SaveSetting("AlphaVantageApiKey", txtApiKey.Text);

                // Save forecasting settings
                AppSettings.SaveSetting("ForecastHorizon", ForecastHorizonTextBox.Text);
                AppSettings.SaveSetting("ConfidenceLevel", ConfidenceLevelTextBox.Text);
                AppSettings.SaveSetting("SafetyStockFactor", SafetyStockFactorTextBox.Text);

                // Save deep learning settings
                AppSettings.SaveSetting("LSTMEpochs", LSTMEpochsTextBox.Text);
                AppSettings.SaveSetting("BatchSize", BatchSizeTextBox.Text);
                AppSettings.SaveSetting("LearningRate", LearningRateTextBox.Text);
                AppSettings.SaveSetting("LSTMUnits", LSTMUnitsTextBox.Text);
                AppSettings.SaveSetting("DropoutRate", DropoutRateTextBox.Text);
                AppSettings.SaveSetting("UseGPU", UseGPUCheckBox.IsChecked.ToString());

                MessageBox.Show("Settings saved successfully. Changes will take effect the next time you run a forecast.",
                               "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private bool ValidateNumericInput(string input, string fieldName, bool isInteger)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show($"{fieldName} cannot be empty.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (isInteger)
            {
                if (!int.TryParse(input, out int value) || value <= 0)
                {
                    MessageBox.Show($"{fieldName} must be a positive integer.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else
            {
                if (!float.TryParse(input, out float value) || value < 0)
                {
                    MessageBox.Show($"{fieldName} must be a non-negative number.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            
            return true;
        }
    }
}
