using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;
using DemandForecastingApp.Data;
using DemandForecastingApp.Models;
using DemandForecastingApp.Services;
using DemandForecastingApp.Utils;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace DemandForecastingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Fields
        private readonly DataImporter _dataImporter;
        private readonly RossmannDataImporter _rossmannDataImporter;
        private readonly MarketDataService _marketDataService;
        private LSTMForecaster? _lstmForecaster;
        private List<RossmannSalesRecord>? _rossmannRecords;
        private string _leadTime;
        private string _reorderThreshold;
        private string _statusMessage;
        private ObservableCollection<ForecastDataPoint> _forecastPoints;
        private ObservableCollection<InventoryRecommendation> _inventoryRecommendations;
        private ObservableCollection<MarketIndicator> _marketData;
        private ObservableCollection<StockQuote> _sectorPerformance;
        private List<string> _labels;
        private string _selectedModelType;
        private bool _isRossmannDataLoaded;
        private IEnumerable<ISeries> _chartSeries;
        private IEnumerable<Axis> _chartXAxes;
        private IEnumerable<Axis> _chartYAxes;

        // Properties
        public ICommand LoadDataCommand { get; }
        public ICommand RunForecastCommand { get; }
        public ICommand FetchMarketDataCommand { get; }

        public string LeadTime
        {
            get => _leadTime;
            set => SetProperty(ref _leadTime, value);
        }

        public string ReorderThreshold
        {
            get => _reorderThreshold;
            set => SetProperty(ref _reorderThreshold, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SelectedModelType
        {
            get => _selectedModelType;
            set => SetProperty(ref _selectedModelType, value);
        }

        public IEnumerable<ISeries> ChartSeries
        {
            get => _chartSeries;
            set => SetProperty(ref _chartSeries, value);
        }

        public IEnumerable<Axis> ChartXAxes
        {
            get => _chartXAxes;
            set => SetProperty(ref _chartXAxes, value);
        }

        public IEnumerable<Axis> ChartYAxes
        {
            get => _chartYAxes;
            set => SetProperty(ref _chartYAxes, value);
        }

        public ObservableCollection<ForecastDataPoint> ForecastPoints
        {
            get => _forecastPoints;
            set => SetProperty(ref _forecastPoints, value);
        }

        public ObservableCollection<InventoryRecommendation> InventoryRecommendations
        {
            get => _inventoryRecommendations;
            set => SetProperty(ref _inventoryRecommendations, value);
        }

        public MainViewModel()
        {
            _dataImporter = new DataImporter { ProductId = "DefaultProduct" };
            _rossmannDataImporter = new RossmannDataImporter();
            _marketDataService = new MarketDataService();
            _leadTime = "3";
            _reorderThreshold = "100";
            _statusMessage = "Ready";
            _selectedModelType = "SSA (Default)";
            _isRossmannDataLoaded = false;

            InitializeCollections();
            InitializeCharts();

            LoadDataCommand = new RelayCommand(LoadData);
            RunForecastCommand = new RelayCommand(RunForecast, CanRunForecast);
            FetchMarketDataCommand = new RelayCommand(async _ => await FetchMarketDataAsync());

            Task.Run(async () => await FetchMarketDataAsync());
        }

        private void InitializeCollections()
        {
            _labels = new List<string>();
            _forecastPoints = new ObservableCollection<ForecastDataPoint>();
            _inventoryRecommendations = new ObservableCollection<InventoryRecommendation>();
            _marketData = new ObservableCollection<MarketIndicator>();
            _sectorPerformance = new ObservableCollection<StockQuote>();
        }

        private void InitializeCharts()
        {
            _chartSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new double[] { },
                    Name = "Forecast",
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 10
                }
            };

            _chartXAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Date",
                    Labels = new string[] { }
                }
            };

            _chartYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Sales"
                }
            };
        }

        private async void LoadData(object? parameter)
        {
            try
            {
                string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                StatusMessage = "Loading Rossmann dataset from Data folder...";
                
                await Task.Run(() =>
                {
                    _rossmannRecords = _rossmannDataImporter.ImportData(dataFolderPath);
                    _rossmannDataImporter.FeatureEngineering();
                    _isRossmannDataLoaded = true;
                });
                
                StatusMessage = $"Loaded {_rossmannRecords?.Count ?? 0} Rossmann sales records";
                
                if (SelectedModelType.Contains("LSTM"))
                {
                    await InitializeLSTMModelAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not automatically load data: {ex.Message}. Prompting for folder selection.");
                StatusMessage = "Please select the Data folder containing Rossmann dataset...";
                
                var openFolderDialog = new FolderBrowserDialog
                {
                    Description = "Select folder containing Rossmann dataset (train.csv, store.csv, etc.)"
                };

                if (openFolderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = openFolderDialog.SelectedPath;
                    StatusMessage = "Loading Rossmann dataset...";
                    
                    try
                    {
                        await Task.Run(() =>
                        {
                            _rossmannRecords = _rossmannDataImporter.ImportData(folderPath);
                            _rossmannDataImporter.FeatureEngineering();
                            _isRossmannDataLoaded = true;
                        });
                        
                        StatusMessage = $"Loaded {_rossmannRecords?.Count ?? 0} Rossmann sales records";
                        
                        if (SelectedModelType.Contains("LSTM"))
                        {
                            await InitializeLSTMModelAsync();
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Logger.LogError("Error loading Rossmann dataset", innerEx);
                        MessageBox.Show($"Error loading data: {innerEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusMessage = "Error loading data";
                    }
                }
                else
                {
                    StatusMessage = "Data loading cancelled";
                }
            }
        }

        private async Task InitializeLSTMModelAsync()
        {
            try
            {
                _lstmForecaster = new LSTMForecaster();
                
                if (_lstmForecaster.HasPreTrainedModel("default"))
                {
                    StatusMessage = "Loading pre-trained LSTM model...";
                    if (_lstmForecaster.LoadModel("default"))
                    {
                        StatusMessage = "Pre-trained LSTM model loaded successfully";
                        return;
                    }
                }
                
                await TrainLSTMModelAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error initializing LSTM model", ex);
                throw;
            }
        }

        private async Task TrainLSTMModelAsync()
        {
            try
            {
                StatusMessage = "Training LSTM model (this may take a while)...";
                
                await Task.Run(() =>
                {
                    if (_lstmForecaster == null)
                    {
                        _lstmForecaster = new LSTMForecaster();
                    }
                    
                    var trainingData = _rossmannRecords?
                        .Where(r => r.Sales.HasValue)
                        .Take(10000)  // Limit for demo purposes
                        .ToList() ?? new List<RossmannSalesRecord>();
                    
                    _lstmForecaster.Train(trainingData, epochs: 5);  // Reduced epochs for demo
                    _lstmForecaster.SaveModel("default");
                });
                
                StatusMessage = "LSTM model training completed and saved";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training LSTM model", ex);
                MessageBox.Show($"Error training LSTM model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error training LSTM model";
            }
        }

        private bool CanRunForecast(object? parameter)
        {
            return !string.IsNullOrEmpty(LeadTime) && !string.IsNullOrEmpty(ReorderThreshold);
        }

        private async void RunForecast(object? parameter)
        {
            if (!int.TryParse(LeadTime, out int leadTime))
            {
                MessageBox.Show("Invalid input for Lead Time. Please enter a number.");
                return;
            }
            if (!int.TryParse(ReorderThreshold, out int reorderThreshold))
            {
                MessageBox.Show("Invalid input for Reorder Threshold. Please enter a number.");
                return;
            }
            // ... (rest of the existing RunForecast implementation)
        }
    }
}
