using System.Windows;
using DemandForecastingApp.ViewModels;

namespace DemandForecastingApp.UI
{
    public partial class SettingsWindow : Window
    {
        private SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            // Handle window closing
            _viewModel.CancelCommand = new System.Windows.Input.RelayCommand(_ => Close());
            _viewModel.SaveSettingsCommand = new System.Windows.Input.RelayCommand(_ =>
            {
                _viewModel.SaveSettings();
                Close();
            });
        }
    }
} 