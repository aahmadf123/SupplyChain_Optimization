using System.Collections.ObjectModel;
using System.Windows.Controls;
using DemandForecastingApp.Models;

namespace DemandForecastingApp.UI
{
    /// <summary>
    /// Interaction logic for ForecastDetailsControl.xaml
    /// </summary>
    public partial class ForecastDetailsControl : UserControl
    {
        private ObservableCollection<ForecastDataPoint> _forecastData;
        
        public ForecastDetailsControl()
        {
            InitializeComponent();
            _forecastData = new ObservableCollection<ForecastDataPoint>();
            ForecastDataGrid.ItemsSource = _forecastData;
        }
        
        public void UpdateForecastData(ObservableCollection<ForecastDataPoint> newData)
        {
            _forecastData.Clear();
            foreach (var item in newData)
            {
                _forecastData.Add(item);
            }
        }
    }
}
