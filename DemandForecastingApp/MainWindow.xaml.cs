using System;
using System.Windows;
using DemandForecastingApp.ViewModels;
using DemandForecastingApp.Models;  // Added for LSTMForecaster, DemandRecord, RossmannSalesRecord

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
            DataContext = new MainViewModel();
        }
    }
}
