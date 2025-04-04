using System.Windows;
using DemandForecastingApp.ViewModels;

namespace DemandForecastingApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
