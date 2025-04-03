using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace DemandForecastingApp.UI
{
    /// <summary>
    /// Interaction logic for InventoryRecommendationsControl.xaml
    /// </summary>
    public partial class InventoryRecommendationsControl : UserControl
    {
        private ObservableCollection<InventoryRecommendation> _recommendations;
        
        public InventoryRecommendationsControl()
        {
            InitializeComponent();
            _recommendations = new ObservableCollection<InventoryRecommendation>();
            RecommendationsListView.ItemsSource = _recommendations;
        }
        
        public void UpdateRecommendations(ObservableCollection<InventoryRecommendation> newRecommendations)
        {
            _recommendations.Clear();
            foreach (var item in newRecommendations)
            {
                _recommendations.Add(item);
            }
        }
    }
}
