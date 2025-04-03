using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Win32;
using LiveCharts;
using LiveCharts.Wpf;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;
using DemandForecastingApp.ViewModels;

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
