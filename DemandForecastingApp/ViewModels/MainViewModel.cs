using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DemandForecastingApp.Data;
using DemandForecastingApp.Models;
using DemandForecastingApp.Services;
using DemandForecastingApp.Utils;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Win32;

namespace DemandForecastingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DataImporter _dataImporter;
        private RossmannDataImporter _rossmannDataImporter;
        private MarketDataService _marketDataService;
        private LSTMForecaster _lstmForecaster;
        private List<Data.Record> _loadedRecords;
        private List<RossmannSalesRecord> _rossmannRecords;
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

        public List<string> ChartLabels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
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
        
        public ObservableCollection<MarketIndicator> MarketData
        {
            get => _marketData;
            set => SetProperty(ref _marketData, value);
        }
        
        public ObservableCollection<StockQuote> SectorPerformance
        {
            get => _sectorPerformance;
            set => SetProperty(ref _sectorPerformance, value);
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

        public MainViewModel()
        {
            _dataImporter = new DataImporter();
            _rossmannDataImporter = new RossmannDataImporter();
            _marketDataService = new MarketDataService();
            
            // Initialize with empty LiveChartsCore series
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
            
            _labels = new List<string>();
            _forecastPoints = new ObservableCollection<ForecastDataPoint>();
            _inventoryRecommendations = new ObservableCollection<InventoryRecommendation>();
            _marketData = new ObservableCollection<MarketIndicator>();
            _sectorPerformance = new ObservableCollection<StockQuote>();
            _statusMessage = "Ready";
            _leadTime = "3";
            _reorderThreshold = "100";
            _selectedModelType = "SSA (Default)";
            _isRossmannDataLoaded = false;

            LoadDataCommand = new RelayCommand(LoadData);
            RunForecastCommand = new RelayCommand(RunForecast, CanRunForecast);
            FetchMarketDataCommand = new RelayCommand(async _ => await FetchMarketDataAsync());
            
            // Initialize with empty market data
            Task.Run(async () => await FetchMarketDataAsync());
        }

        private bool CanRunForecast(object? parameter)
        {
            return !string.IsNullOrEmpty(LeadTime) && !string.IsNullOrEmpty(ReorderThreshold);
        }

        private async void LoadData(object? parameter)
        {
            try
            {
                StatusMessage = "Loading Rossmann dataset from Data folder...";
                
                await Task.Run(() =>
                {
                    // Load Rossmann dataset directly from the Data folder
                    _rossmannRecords = _rossmannDataImporter.ImportData();
                    
                    // Perform feature engineering
                    _rossmannRecords = _rossmannDataImporter.FeatureEngineering(_rossmannRecords);
                    
                    _isRossmannDataLoaded = true;
                });
                
                StatusMessage = $"Loaded {_rossmannRecords.Count} Rossmann sales records";
                
                // If LSTM is selected, train model (this can take time)
                if (SelectedModelType.Contains("LSTM"))
                {
                    await TrainLSTMModelAsync();
                }
            }
            catch (Exception ex)
            {
                // If automatic loading fails, prompt user to select folder
                Logger.LogWarning($"Could not automatically load data: {ex.Message}. Prompting for folder selection.");
                StatusMessage = "Please select the Data folder containing Rossmann dataset...";
                
                var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select folder containing Rossmann dataset (train.csv, store.csv, etc.)"
                };

                if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = openFolderDialog.SelectedPath;
                    StatusMessage = "Loading Rossmann dataset...";
                    
                    try
                    {
                        await Task.Run(() =>
                        {
                            // Load Rossmann dataset from selected folder
                            _rossmannRecords = _rossmannDataImporter.ImportData(folderPath);
                            
                            // Perform feature engineering
                            _rossmannRecords = _rossmannDataImporter.FeatureEngineering(_rossmannRecords);
                            
                            _isRossmannDataLoaded = true;
                        });
                        
                        StatusMessage = $"Loaded {_rossmannRecords.Count} Rossmann sales records";
                        
                        // If LSTM is selected, train model (this can take time)
                        if (SelectedModelType.Contains("LSTM"))
                        {
                            await TrainLSTMModelAsync();
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

        private async Task TrainLSTMModelAsync()
        {
            try
            {
                StatusMessage = "Training LSTM model (this may take a while)...";
                
                await Task.Run(() =>
                {
                    _lstmForecaster = new LSTMForecaster();
                    
                    // Use a subset for training to keep it manageable
                    var trainingData = _rossmannRecords
                        .Where(r => r.Sales.HasValue)
                        .Take(10000)  // Limit for demo purposes
                        .ToList();
                    
                    _lstmForecaster.Train(trainingData, epochs: 5);  // Reduced epochs for demo
                });
                
                StatusMessage = "LSTM model training completed";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error training LSTM model", ex);
                MessageBox.Show($"Error training LSTM model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error training LSTM model";
            }
        }

        private async void RunForecast(object? parameter)
        {
            // Validate the input parameters for Lead Time and Reorder Threshold.
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

            try
            {
                List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults;
                
                if (_isRossmannDataLoaded)
                {
                    // Use Rossmann data with selected model
                    if (SelectedModelType.Contains("LSTM"))
                    {
                        if (_lstmForecaster == null)
                        {
                            await TrainLSTMModelAsync();
                        }
                        
                        // Use LSTM forecaster
                        StatusMessage = "Running LSTM forecast...";
                        forecastResults = await Task.Run(() => _lstmForecaster.PredictSales(
                            _rossmannRecords.Where(r => r.Sales.HasValue).Take(1000).ToList(),
                            10 // horizon days
                        ));
                    }
                    else
                    {
                        // Use SSA forecaster with Rossmann data
                        StatusMessage = "Running SSA forecast on Rossmann data...";
                        
                        // Convert Rossmann records to DemandRecord format
                        var demandRecords = _rossmannRecords
                            .Where(r => r.Sales.HasValue)
                            .Take(1000)  // Limit for demo
                            .Select(r => new DemandRecord
                            {
                                Date = r.Date,
                                Demand = r.Sales.Value
                            })
                            .ToList();
                        
                        var ssaForecaster = new ForecastModel();
                        forecastResults = ssaForecaster.PredictDemand(demandRecords, 10);
                    }
                }
                else
                {
                    // Use demo data if no Rossmann data is loaded
                    forecastResults = GenerateDemoForecastData();
                }
                
                // Update the chart with LiveChartsCore
                UpdateForecastChart(forecastResults);
                
                // Update forecast details and inventory recommendations
                UpdateForecastDetails(forecastResults, leadTime, reorderThreshold);
                
                // Update market data
                await FetchMarketDataAsync();
                
                // Log success
                Logger.LogInfo($"Forecast completed: Lead Time={leadTime}, Reorder Threshold={reorderThreshold}");
                
                StatusMessage = $"Forecast ran with Lead Time: {leadTime}, Reorder Threshold: {reorderThreshold}";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> GenerateDemoForecastData()
        {
            // Create demo forecast data
            var startDate = DateTime.Now;
            var results = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
            
            var baseValue = 100f;
            var random = new Random(123); // Fixed seed for reproducibility
            
            for (int i = 0; i < 10; i++)
            {
                var date = startDate.AddDays(i);
                var forecast = baseValue + (i * 5) + (float)(random.NextDouble() * 10 - 5);
                var lowerBound = forecast * 0.85f;
                var upperBound = forecast * 1.15f;
                
                results.Add((date, forecast, lowerBound, upperBound));
            }
            
            return results;
        }
        
        private void UpdateForecastDetails(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults, 
                                           int leadTime, int reorderThreshold)
        {
            // Create forecast data points
            var forecastDetails = new ObservableCollection<ForecastDataPoint>();
            
            foreach (var result in forecastResults)
            {
                forecastDetails.Add(new ForecastDataPoint
                {
                    Period = result.Date.ToString("MMM dd"),
                    Date = result.Date,
                    ForecastedDemand = result.Forecast,
                    LowerBound = result.LowerBound,
                    UpperBound = result.UpperBound,
                    ReorderPoint = result.Forecast < reorderThreshold ? "Yes" : "No"
                });
            }
            
            ForecastPoints = forecastDetails;
            
            // Update inventory recommendations
            UpdateInventoryRecommendations(forecastResults, leadTime, reorderThreshold);
        }
        
        private void UpdateInventoryRecommendations(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults,
                                                     int leadTime, int reorderThreshold)
        {
            var recommendations = new ObservableCollection<InventoryRecommendation>();
            
            // For demo, use store IDs from Rossmann if available
            var storeIds = _isRossmannDataLoaded
                ? _rossmannRecords.Select(r => r.StoreId).Distinct().Take(5).ToList()
                : new List<int> { 1, 2, 3, 4, 5 };
            
            foreach (var storeId in storeIds)
            {
                // Calculate total forecasted demand during lead time
                double leadTimeDemand = forecastResults.Take(leadTime).Sum(r => r.Forecast);
                
                // Calculate safety stock based on variability
                double stdDev = CalculateStandardDeviation(forecastResults.Select(r => (double)r.Forecast));
                double safetyStock = stdDev * 1.65; // 95% service level
                
                // Calculate recommended order
                double currentStock = 10 + (storeId * 2); // Mock value
                double recommendedOrder = Math.Max(0, leadTimeDemand + safetyStock - currentStock);
                
                recommendations.Add(new InventoryRecommendation
                {
                    Item = $"Store {storeId}",
                    CurrentStock = (int)currentStock,
                    RecommendedOrder = (int)Math.Ceiling(recommendedOrder),
                    LeadTimeDemand = Math.Round(leadTimeDemand, 2),
                    SafetyStock = Math.Round(safetyStock, 2)
                });
            }
            
            InventoryRecommendations = recommendations;
        }
        
        private async Task FetchMarketDataAsync(object? parameter = null)
        {
            try
            {
                // Update status
                StatusMessage = "Fetching market data...";
                
                // Fetch economic indicators
                var indicators = await _marketDataService.GetMarketIndicatorsAsync();
                MarketData = new ObservableCollection<MarketIndicator>(indicators);
                
                // Fetch sector performance
                var sectors = await _marketDataService.GetSectorPerformanceAsync();
                SectorPerformance = new ObservableCollection<StockQuote>(sectors);
                
                StatusMessage = "Market data updated";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching market data", ex);
                StatusMessage = "Error fetching market data";
            }
        }
        
        // Helper method to calculate standard deviation
        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();
            var avg = enumerable.Average();
            var sum = enumerable.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (enumerable.Length - 1));
        }

        public async Task<Dictionary<string, double>> AnalyzeMarketCorrelations()
        {
            try
            {
                if (ForecastPoints == null || ForecastPoints.Count == 0 || MarketData == null || MarketData.Count == 0)
                {
                    return new Dictionary<string, double>();
                }
                
                // Get forecast values
                var forecastValues = ForecastPoints.Select(p => p.ForecastedDemand).ToList();
                
                // Create a dictionary to store correlations
                var correlations = new Dictionary<string, double>();
                
                // Get sample market data values for correlation
                foreach (var indicator in MarketData)
                {
                    // Try to parse the value as a double
                    if (double.TryParse(indicator.Value.Replace("%", ""), out double value))
                    {
                        // This is just a mock correlation as we don't have historical data
                        // In a real app, you would compare historical market data with historical sales
                        double correlation = CalculateMockCorrelation(value, forecastValues.Average());
                        correlations.Add(indicator.Key, Math.Round(correlation, 2));
                    }
                }
                
                return correlations;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error analyzing market correlations", ex);
                return new Dictionary<string, double>();
            }
        }

        private double CalculateMockCorrelation(double marketValue, double forecastAverage)
        {
            // This is a simplified mock correlation function
            // In a real app, you would use Pearson correlation coefficient with historical data
            var random = new Random();
            double baseCorrelation = random.NextDouble() * 2 - 1;  // Between -1 and 1
            
            // Adjust correlation based on the relative values
            double adjustment = (marketValue - forecastAverage) / (marketValue + forecastAverage);
            
            // Return a value between -1 and 1
            return Math.Max(-1, Math.Min(1, baseCorrelation + adjustment * 0.2));
        }

        private void UpdateForecastChart(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults)
        {
            var labels = forecastResults.Select(r => r.Date.ToString("MMM dd")).ToArray();
            
            var forecastValues = forecastResults.Select(r => (double)r.Forecast).ToArray();
            var lowerBoundValues = forecastResults.Select(r => (double)r.LowerBound).ToArray();
            var upperBoundValues = forecastResults.Select(r => (double)r.UpperBound).ToArray();
            
            ChartSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = forecastValues,
                    Name = "Forecast",
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                    GeometrySize = 6
                },
                new LineSeries<double>
                {
                    Values = lowerBoundValues,
                    Name = "Lower Bound",
                    Stroke = new SolidColorPaint(SKColors.LightBlue, 2),
                    GeometrySize = 0
                },
                new LineSeries<double>
                {
                    Values = upperBoundValues,
                    Name = "Upper Bound",
                    Stroke = new SolidColorPaint(SKColors.LightBlue, 2),
                    GeometrySize = 0
                }
            };
            
            ChartXAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Date",
                    Labels = labels,
                    TextSize = 12,
                    LabelsRotation = 45
                }
            };
            
            ChartYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Sales",
                    TextSize = 12
                }
            };
        }
    }
}