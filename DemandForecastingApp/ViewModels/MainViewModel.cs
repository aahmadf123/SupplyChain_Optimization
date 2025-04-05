using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using DemandForecastingApp.Data;
using DemandForecastingApp.Models;
using DemandForecastingApp.Services;
using DemandForecastingApp.Utils;
using MessageBox = System.Windows.MessageBox;

namespace DemandForecastingApp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DataImporter _dataImporter;
        private readonly RossmannDataImporter _rossmannDataImporter;
        private readonly MarketDataService _marketDataService;
        private LSTMForecaster _lstmForecaster;
        private List<RossmannSalesRecord> _rossmannRecords;
        private string _leadTime = "5";
        private string _reorderThreshold = "20";
        private string _statusMessage = "Ready to load data";
        private ObservableCollection<ForecastDataPoint> _forecastPoints;
        private ObservableCollection<InventoryRecommendation> _inventoryRecommendations;
        private ObservableCollection<MarketIndicator> _marketData;
        private ObservableCollection<StockQuote> _sectorPerformance;
        private List<string> _labels;
        private string _selectedModelType = "SSA (Default)";
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
            _dataImporter = new DataImporter { ProductId = "DefaultProduct" };
            _rossmannDataImporter = new RossmannDataImporter();
            _marketDataService = new MarketDataService();
            _forecastPoints = new ObservableCollection<ForecastDataPoint>();
            _inventoryRecommendations = new ObservableCollection<InventoryRecommendation>();
            _marketData = new ObservableCollection<MarketIndicator>();
            _sectorPerformance = new ObservableCollection<StockQuote>();
            _labels = new List<string>();
            
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
            
            // Initialize commands
            LoadDataCommand = new RelayCommand(LoadData, param => true);
            RunForecastCommand = new RelayCommand(RunForecast, CanRunForecast);
            FetchMarketDataCommand = new RelayCommand(async _ => await FetchMarketDataAsync(), param => true);
        }

        private bool CanRunForecast(object parameter)
        {
            return !string.IsNullOrEmpty(LeadTime) && !string.IsNullOrEmpty(ReorderThreshold);
        }

        private async void LoadData(object parameter)
        {
            try
            {
                string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                StatusMessage = "Loading Rossmann dataset from Data folder...";
                
                await Task.Run(() =>
                {
                    try
                    {
                        _rossmannRecords = _rossmannDataImporter.ImportData(dataFolderPath);
                        _rossmannDataImporter.FeatureEngineering();
                        _isRossmannDataLoaded = true;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Data folder not found, will prompt user
                        _isRossmannDataLoaded = false;
                        throw;
                    }
                });
                
                StatusMessage = $"Loaded {_rossmannRecords?.Count ?? 0} Rossmann sales records";
                
                if (SelectedModelType.Contains("LSTM"))
                {
                    await TrainLSTMModelAsync();
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
                    var trainingData = _rossmannRecords?
                        .Where(r => r.Sales.HasValue)
                        .Take(10000)  // Limit for demo purposes
                        .ToList() ?? new List<RossmannSalesRecord>();
                    
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

        private async void RunForecast(object parameter)
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
                        forecastResults = await Task.Run(() => _lstmForecaster?.PredictSales(
                            _rossmannRecords?.Where(r => r.Sales.HasValue).Take(1000).ToList() ?? new List<RossmannSalesRecord>(),
                            10 // horizon days
                        ) ?? new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>());
                    }
                    else
                    {
                        // Use SSA forecaster with Rossmann data
                        StatusMessage = "Running SSA forecast on Rossmann data...";

                        // Convert Rossmann records to DemandRecord format
                        var demandRecords = _rossmannRecords?
                            .Where(r => r.Sales.HasValue)
                            .Take(1000)  // Limit for demo
                            .Select(r => new DemandRecord
                            {
                                Date = r.Date,
                                Sales = r.Sales ?? 0,
                                StateHoliday = r.StateHoliday ?? "0"  // Default to "0" if null, matching Rossmann dataset format
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
                Logger.LogInfo("Forecast completed successfully");
                StatusMessage = "Forecast completed successfully";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error running forecast", ex);
                MessageBox.Show($"Error running forecast: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error running forecast";
            }
        }

        private void UpdateForecastChart(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults)
        {
            try
            {
                if (forecastResults == null || forecastResults.Count == 0)
                {
                    return;
                }

                // Extract data for chart
                var dates = forecastResults.Select(r => r.Date.ToString("MM/dd")).ToArray();
                var forecasts = forecastResults.Select(r => (double)r.Forecast).ToArray();
                var lowerBounds = forecastResults.Select(r => (double)r.LowerBound).ToArray();
                var upperBounds = forecastResults.Select(r => (double)r.UpperBound).ToArray();

                // Create series for the chart
                var series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = forecasts,
                        Name = "Forecast",
                        Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                        GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        GeometrySize = 10
                    },
                    new LineSeries<double>
                    {
                        Values = lowerBounds,
                        Name = "Lower Bound",
                        Stroke = new SolidColorPaint(SKColors.LightGray, 2),
                        GeometrySize = 0
                    },
                    new LineSeries<double>
                    {
                        Values = upperBounds,
                        Name = "Upper Bound",
                        Stroke = new SolidColorPaint(SKColors.LightGray, 2),
                        GeometrySize = 0
                    }
                };

                // Create area between upper and lower bounds
                var areaSeries = new StackedAreaSeries<double>
                {
                    Values = upperBounds.Zip(lowerBounds, (u, l) => u - l).ToArray(),
                    Name = "Confidence Interval",
                    Stroke = null,
                    Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(40)),
                    GeometrySize = 0,
                    ZIndex = -1 // Draw behind other series
                };

                // Update chart axes
                var xAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = "Date",
                        Labels = dates,
                        LabelsRotation = 45
                    }
                };

                var yAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = "Sales"
                    }
                };

                // Update chart properties
                ChartSeries = series;
                ChartXAxes = xAxes;
                ChartYAxes = yAxes;

                Logger.LogInfo("Chart updated successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error updating forecast chart", ex);
                StatusMessage = "Error updating forecast chart";
            }
        }

        private void UpdateForecastDetails(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults,
                                          int leadTime, int reorderThreshold)
        {
            try
            {
                // Clear existing collections
                ForecastPoints.Clear();
                InventoryRecommendations.Clear();

                // Calculate safety stock factor (using 95% confidence level = 1.65)
                float safetyFactor = 1.65f;

                // Get safety stock factor from settings if available
                try
                {
                    string safetyFactorStr = AppSettings.GetSetting("SafetyStockFactor", "1.65");
                    if (float.TryParse(safetyFactorStr, out float configuredFactor))
                    {
                        safetyFactor = configuredFactor;
                    }
                }
                catch
                {
                    // Use default if settings can't be loaded
                }

                // Calculate standard deviation of forecast error (simplified)
                float stdDevForecastError = forecastResults
                    .Select(r => r.UpperBound - r.Forecast)
                    .Average() / safetyFactor;

                // Populate forecast points
                foreach (var result in forecastResults)
                {
                    ForecastPoints.Add(new ForecastDataPoint
                    {
                        Period = result.Date.ToString("MM/dd/yyyy"),
                        ForecastedDemand = result.Forecast,
                        LowerBound = result.LowerBound,
                        UpperBound = result.UpperBound,
                        ReorderPoint = CalculateLeadTimeDemand(result.Forecast, leadTime) + 
                                      safetyFactor * stdDevForecastError * (float)Math.Sqrt(leadTime)
                    });
                }

                // Generate inventory recommendations based on forecast
                GenerateInventoryRecommendations(forecastResults, leadTime, reorderThreshold, safetyFactor, stdDevForecastError);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error updating forecast details", ex);
                MessageBox.Show($"Error updating forecast details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private float CalculateLeadTimeDemand(float dailyDemand, int leadTime)
        {
            return dailyDemand * leadTime;
        }

        private void GenerateInventoryRecommendations(List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> forecastResults,
                                                     int leadTime, int reorderThreshold, float safetyFactor, float stdDevForecastError)
        {
            try
            {
                // For demonstration, we'll create recommendations for different store types
                var storeTypes = new[] { "A", "B", "C", "D" };
                var random = new Random(42); // Seed for reproducibility

                foreach (var storeType in storeTypes)
                {
                    // For demo purposes, generate random values
                    int currentStock = random.Next(10, 50);
                    
                    // Calculate average daily demand from forecast
                    float avgDailyDemand = forecastResults.Average(r => r.Forecast);
                    
                    // Calculate lead time demand
                    float leadTimeDemand = CalculateLeadTimeDemand(avgDailyDemand, leadTime);
                    
                    // Calculate safety stock
                    float safetyStock = safetyFactor * stdDevForecastError * (float)Math.Sqrt(leadTime);
                    
                    // Calculate reorder point
                    float reorderPoint = leadTimeDemand + safetyStock;
                    
                    // Calculate recommended order quantity
                    int recommendedOrder = 0;
                    if (currentStock <= reorderPoint)
                    {
                        // If current stock is below reorder point, order enough to reach optimal level
                        recommendedOrder = (int)Math.Ceiling(reorderPoint + leadTimeDemand - currentStock);
                    }

                    // Add recommendation
                    InventoryRecommendations.Add(new InventoryRecommendation
                    {
                        Item = storeType,
                        CurrentStock = currentStock,
                        ReorderPoint = reorderPoint,
                        LeadTimeDemand = leadTimeDemand,
                        SafetyStock = safetyStock,
                        RecommendedOrder = recommendedOrder
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error generating inventory recommendations", ex);
                throw;
            }
        }

        private async Task FetchMarketDataAsync()
        {
            try
            {
                StatusMessage = "Fetching market data...";

                await Task.Run(() =>
                {
                    // Clear existing collections
                    MarketData.Clear();
                    SectorPerformance.Clear();

                    // Get API key from settings
                    string apiKey = AppSettings.GetSetting("AlphaVantageApiKey", "APFCJIALTDC7YYUT");

                    try
                    {
                        // Fetch market data using the service
                        var marketIndicators = _marketDataService.GetMarketIndicators(apiKey);
                        var sectorPerformance = _marketDataService.GetSectorPerformance(apiKey);

                        // Update UI collections
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var indicator in marketIndicators)
                            {
                                MarketData.Add(indicator);
                            }

                            foreach (var sector in sectorPerformance)
                            {
                                SectorPerformance.Add(sector);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error fetching real market data: {ex.Message}. Using demo data.");

                        // Use demo data if API call fails
                        var demoMarketData = GenerateDemoMarketData();
                        var demoSectorData = GenerateDemoSectorData();

                        // Update UI collections
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var indicator in demoMarketData)
                            {
                                MarketData.Add(indicator);
                            }

                            foreach (var sector in demoSectorData)
                            {
                                SectorPerformance.Add(sector);
                            }
                        });
                    }
                });

                StatusMessage = "Market data updated";
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching market data", ex);
                StatusMessage = "Error fetching market data";
            }
        }

        private List<MarketIndicator> GenerateDemoMarketData()
        {
            return new List<MarketIndicator>
            {
                new MarketIndicator { Key = "S&P 500", Value = "4,238.45", Change = "+1.2%", Impact = "Positive" },
                new MarketIndicator { Key = "Crude Oil", Value = "$72.85", Change = "-0.8%", Impact = "Negative" },
                new MarketIndicator { Key = "10-Year Treasury", Value = "1.89%", Change = "+0.05%", Impact = "Negative" },
                new MarketIndicator { Key = "Gold", Value = "$1,845.20", Change = "+0.3%", Impact = "Positive" },
                new MarketIndicator { Key = "USD Index", Value = "96.54", Change = "-0.2%", Impact = "Positive" },
                new MarketIndicator { Key = "VIX", Value = "18.92", Change = "-5.1%", Impact = "Positive" }
            };
        }

        private List<StockQuote> GenerateDemoSectorData()
        {
            return new List<StockQuote>
            {
                new StockQuote { Symbol = "Consumer Staples", Price = 78.42m, Change = "+1.2%" },
                new StockQuote { Symbol = "Technology", Price = 156.78m, Change = "+2.3%" },
                new StockQuote { Symbol = "Healthcare", Price = 112.34m, Change = "+0.8%" },
                new StockQuote { Symbol = "Energy", Price = 65.21m, Change = "-1.5%" },
                new StockQuote { Symbol = "Financials", Price = 92.67m, Change = "+0.5%" },
                new StockQuote { Symbol = "Industrials", Price = 104.89m, Change = "-0.3%" },
                new StockQuote { Symbol = "Materials", Price = 85.34m, Change = "+0.7%" },
                new StockQuote { Symbol = "Utilities", Price = 68.92m, Change = "-0.2%" },
                new StockQuote { Symbol = "Real Estate", Price = 42.56m, Change = "+1.1%" }
            };
        }

        private List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)> GenerateDemoForecastData()
        {
            var startDate = DateTime.Today;
            var random = new Random(42);
            var forecasts = new List<(DateTime Date, float Forecast, float LowerBound, float UpperBound)>();
            
            float baseValue = 100;
            
            for (int i = 0; i < 10; i++)
            {
                var date = startDate.AddDays(i);
                var forecast = baseValue + i * 2 + (float)(random.NextDouble() * 5 - 2.5);
                var error = (float)(forecast * 0.1); // 10% error margin
                
                forecasts.Add((
                    date,
                    forecast,
                    forecast - error,
                    forecast + error
                ));
                
                baseValue = forecast; // Trend follows previous forecast
            }
            
            return forecasts;
        }
    }
}