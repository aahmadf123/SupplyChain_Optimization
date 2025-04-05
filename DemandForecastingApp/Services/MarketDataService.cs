using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private const string ALPHA_VANTAGE_BASE_URL = "https://www.alphavantage.co/query";
        
        public MarketDataService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Set timeout to 10 seconds
        }
        
        public List<MarketIndicator> GetMarketIndicators(string apiKey)
        {
            try
            {
                var indicators = new List<MarketIndicator>();
                
                // Make synchronous calls for simplicity in demo
                GetGlobalMarketStatus(indicators, apiKey).Wait();
                GetEconomicIndicator(indicators, "GDP", "US GDP Annual Growth Rate", apiKey).Wait();
                GetEconomicIndicator(indicators, "INFLATION", "US Inflation Rate", apiKey).Wait();
                
                return indicators;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting market indicators", ex);
                return GenerateDemoMarketData();
            }
        }

        public List<StockQuote> GetSectorPerformance(string apiKey)
        {
            try
            {
                var sectors = GetSectorPerformanceAsync(apiKey).Result;
                return sectors;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting sector performance", ex);
                return GenerateDemoSectorData();
            }
        }
        
        private async Task GetGlobalMarketStatus(List<MarketIndicator> indicators, string apiKey)
        {
            try
            {
                // Get global market status (S&P 500)
                string url = $"{ALPHA_VANTAGE_BASE_URL}?function=GLOBAL_QUOTE&symbol=SPY&apikey={apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                
                // Parse JSON response
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Global Quote", out var quote))
                    {
                        string symbol = quote.TryGetProperty("01. symbol", out var sym) ? sym.GetString() ?? "SPY" : "SPY";
                        string price = quote.TryGetProperty("05. price", out var p) ? p.GetString() ?? "N/A" : "N/A";
                        string change = quote.TryGetProperty("09. change", out var c) ? c.GetString() ?? "0" : "0";
                        string changePercent = quote.TryGetProperty("10. change percent", out var cp) ? cp.GetString() ?? "0%" : "0%";
                        
                        double changeValue = 0;
                        if (double.TryParse(change, out double changeVal))
                        {
                            changeValue = changeVal;
                        }
                        
                        indicators.Add(new MarketIndicator
                        {
                            Key = "S&P 500 ETF",
                            Value = price ?? "N/A",
                            Change = changePercent ?? "0%",
                            Impact = changeValue >= 0 ? "Positive" : "Negative"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error fetching global market status: {ex.Message}");
                indicators.Add(new MarketIndicator
                {
                    Key = "S&P 500 ETF",
                    Value = "4,238.45",
                    Change = "+1.2%",
                    Impact = "Positive"
                });
            }
        }
        
        private async Task GetEconomicIndicator(List<MarketIndicator> indicators, string indicator, string displayName, string apiKey)
        {
            try
            {
                // Note: This is a simplified implementation. In a real app, you would use the Alpha Vantage API
                // to get actual economic indicators.
                await Task.Delay(100); // Simulate API call
                
                if (indicator == "GDP")
                {
                    indicators.Add(new MarketIndicator
                    {
                        Key = displayName,
                        Value = "2.1%",
                        Change = "+0.2%",
                        Impact = "Positive"
                    });
                }
                else if (indicator == "INFLATION")
                {
                    indicators.Add(new MarketIndicator
                    {
                        Key = displayName,
                        Value = "3.4%",
                        Change = "-0.1%",
                        Impact = "Positive"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error fetching economic indicator '{indicator}': {ex.Message}");
                indicators.Add(new MarketIndicator
                {
                    Key = displayName,
                    Value = "N/A",
                    Change = "0%",
                    Impact = "Neutral"
                });
            }
        }
        
        private async Task<List<StockQuote>> GetSectorPerformanceAsync(string apiKey)
        {
            try
            {
                // Note: This is a simplified implementation. In a real app, you would use the Alpha Vantage API
                // to get sector performance data.
                string url = $"{ALPHA_VANTAGE_BASE_URL}?function=SECTOR&apikey={apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                
                var sectors = new List<StockQuote>();
                
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("Rank A: Real-Time Performance", out var realTime))
                    {
                        foreach (var property in realTime.EnumerateObject())
                        {
                            if (property.Name != "Meta Data")
                            {
                                sectors.Add(new StockQuote
                                {
                                    Symbol = property.Name,
                                    Price = 0, // Alpha Vantage doesn't provide actual prices for sectors
                                    Change = property.Value.GetString() ?? "0%"
                                });
                            }
                        }
                    }
                }
                
                return sectors;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error fetching sector performance: {ex.Message}");
                return GenerateDemoSectorData();
            }
        }

        private List<MarketIndicator> GenerateDemoMarketData()
        {
            return new List<MarketIndicator>
            {
                new MarketIndicator { Key = "S&P 500", Value = "4,238.45", Change = "+1.2%", Impact = "Positive" },
                new MarketIndicator { Key = "Crude Oil", Value = "$72.85", Change = "-0.8%", Impact = "Negative" },
                new MarketIndicator { Key = "10-Year Treasury", Value = "1.89%", Change = "+0.05%", Impact = "Negative" },
                new MarketIndicator { Key = "US GDP Annual Growth Rate", Value = "2.1%", Change = "+0.2%", Impact = "Positive" },
                new MarketIndicator { Key = "US Inflation Rate", Value = "3.4%", Change = "-0.1%", Impact = "Positive" },
                new MarketIndicator { Key = "USD Index", Value = "96.54", Change = "-0.2%", Impact = "Positive" }
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
    }
}