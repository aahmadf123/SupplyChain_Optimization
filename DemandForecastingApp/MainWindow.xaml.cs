using System;
using System.Windows;
using DemandForecastingApp.ViewModels;
using DemandForecastingApp.Models; // Import the Models namespace

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
