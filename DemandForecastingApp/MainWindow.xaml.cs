using System.Windows;
using DemandForecastingApp.ViewModels;

namespace DemandForecastingApp
{
    public partial class MainWindow : Window
    {
        private LSTMForecaster? _lstmForecaster;
        private List<DemandRecord>? _loadedRecords;
        private List<RossmannSalesRecord>? _rossmannRecords;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
