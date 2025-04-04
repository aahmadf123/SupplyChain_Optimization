using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string ALPHA_VANTAGE_BASE_URL = "https://www.alphavantage.co/query";
        
        public MarketDataService()
        {
            _httpClient = new HttpClient();
            _apiKey = AppSettings.GetSetting("AlphaVantageApiKey", "APFCJIALTDC7YYUT");
        }
        
        public async Task<List<MarketIndicator>> GetMarketIndicatorsAsync()
        {
            try
            {
                var indicators = new List<MarketIndicator>();
                
                // Get real market indicators using Alpha Vantage API
                await GetGlobalMarketStatus(indicators);
                await GetEconomicIndicator(indicators, "REAL_GDP", "Real GDP");
                await GetEconomicIndicator(indicators, "CPI", "Consumer Price Index");
                await GetEconomicIndicator(indicators, "RETAIL_SALES", "Retail Sales");
                await GetEconomicIndicator(indicators, "UNEMPLOYMENT", "Unemployment Rate");
                
                return indicators;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching market indicators", ex);
                
                // Fallback to mock data in case of API failure
                var fallbackIndicators = new List<MarketIndicator>();
                AddMockMarketData(fallbackIndicators);
                return fallbackIndicators;
            }
        }
        
        private async Task GetGlobalMarketStatus(List<MarketIndicator> indicators)
        {
            try
            {
                // Get global market status (S&P 500)
                string url = $"{ALPHA_VANTAGE_BASE_URL}?function=GLOBAL_QUOTE&symbol=SPY&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                
                // Parse JSON response
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Global Quote", out var quote))
                    {
                        string symbol = quote.TryGetProperty("01. symbol", out var sym) ? sym.GetString() : "SPY";
                        string price = quote.TryGetProperty("05. price", out var p) ? p.GetString() : "N/A";
                        string change = quote.TryGetProperty("09. change", out var c) ? c.GetString() : "0";
                        string changePercent = quote.TryGetProperty("10. change percent", out var cp) ? cp.GetString() : "0%";
                        
                        double changeValue = 0;
                        if (double.TryParse(change, out double changeVal))
                        {
                            changeValue = changeVal;
                        }
                        
                        indicators.Add(new MarketIndicator
                        {
                            Key = "S&P 500 ETF",
                            Value = price,
                            Change = changePercent,
                            Impact = changeValue >= 0 ? "Positive" : "Negative"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error fetching global market status: {ex.Message}");
                // Add fallback data
                indicators.Add(new MarketIndicator
                {
                    Key = "S&P 500 ETF",
                    Value = "420.75",
                    Change = "+0.5%",
                    Impact = "Positive"
                });
            }
        }
        
        private async Task GetEconomicIndicator(List<MarketIndicator> indicators, string indicator, string displayName)
        {
            try
            {
                string url = $"{ALPHA_VANTAGE_BASE_URL}?function={indicator}&interval=annual&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                
                // Parse JSON response
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var data) && data.GetArrayLength() >= 2)
                    {
                        var latest = data[0];
                        var previous = data[1];
                        
                        string latestDate = latest.TryGetProperty("date", out var date) ? date.GetString() : "";
                        string latestValue = latest.TryGetProperty("value", out var val) ? val.GetString() : "N/A";
                        string previousValue = previous.TryGetProperty("value", out var prevVal) ? prevVal.GetString() : "N/A";
                        
                        double change = 0;
                        double changePercent = 0;
                        if (double.TryParse(latestValue, out double latestVal) && 
                            double.TryParse(previousValue, out double prevValDouble) && 
                            prevValDouble != 0)
                        {
                            change = latestVal - prevValDouble;
                            changePercent = (change / prevValDouble) * 100;
                        }
                        
                        string impact = "Neutral";
                        // Customize impact logic based on the indicator
                        switch (indicator)
                        {
                            case "REAL_GDP":
                            case "RETAIL_SALES":
                                impact = changePercent >= 0 ? "Positive" : "Negative";
                                break;
                            case "CPI":
                            case "UNEMPLOYMENT":
                                impact = changePercent <= 0 ? "Positive" : "Negative";
                                break;
                        }
                        
                        indicators.Add(new MarketIndicator
                        {
                            Key = displayName,
                            Value = latestValue,
                            Change = $"{changePercent:+0.00;-0.00;0.00}%",
                            Impact = impact
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error fetching {displayName}: {ex.Message}");
                // Add fallback data
                AddMockIndicator(indicators, displayName);
            }
        }
        
        public async Task<List<StockQuote>> GetSectorPerformanceAsync()
        {
            try
            {
                string url = $"{ALPHA_VANTAGE_BASE_URL}?function=SECTOR&apikey={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var quotes = new List<StockQuote>();
                
                // Parse JSON response
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var root = doc.RootElement;
                    
                    // Process the "Rank A: Real-Time Performance" section
                    if (root.TryGetProperty("Rank A: Real-Time Performance", out var realtimePerf))
                    {
                        foreach (var property in realtimePerf.EnumerateObject())
                        {
                            var sectorName = property.Name;
                            var performance = property.Value.GetString();
                            
                            quotes.Add(new StockQuote
                            {
                                Symbol = sectorName,
                                Price = "N/A", // Not available in sector performance
                                Change = performance
                            });
                        }
                    }
                }
                
                return quotes;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching sector performance", ex);
                return new List<StockQuote>(); // Return empty list on error
            }
        }
        
        // Method to get weather data for a specific location using OpenWeatherMap API
        // (You would need an API key for OpenWeatherMap to make this work)
        public async Task<WeatherData> GetWeatherDataAsync(string location)
        {
            try
            {
                // For now, return mock data as we don't have OpenWeatherMap API key
                await Task.Delay(300);
                
                var random = new Random();
                
                return new WeatherData
                {
                    Location = location,
                    Temperature = (float)(10 + random.NextDouble() * 20),
                    Condition = GetRandomWeatherCondition(random),
                    Precipitation = (float)(random.NextDouble() * 10)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching weather data for {location}", ex);
                throw;
            }
        }
        
        private void AddMockMarketData(List<MarketIndicator> indicators)
        {
            // Add mock data for when API fails
            AddMockIndicator(indicators, "Consumer Price Index");
            AddMockIndicator(indicators, "Retail Sales Growth");
            AddMockIndicator(indicators, "Real GDP");
            AddMockIndicator(indicators, "Consumer Confidence");
            AddMockIndicator(indicators, "Unemployment Rate");
        }
        
        private void AddMockIndicator(List<MarketIndicator> indicators, string name)
        {
            var random = new Random();
            bool isPositive = random.NextDouble() > 0.5;
            
            switch (name)
            {
                case "Consumer Price Index":
                    indicators.Add(new MarketIndicator
                    {
                        Key = name,
                        Value = $"{100 + random.NextDouble() * 10:F2}",
                        Change = $"{(random.NextDouble() * 2 - 1):+0.00;-0.00;0.00}%",
                        Impact = isPositive ? "Positive" : "Negative"
                    });
                    break;
                    
                case "Retail Sales Growth":
                    indicators.Add(new MarketIndicator
                    {
                        Key = name,
                        Value = $"{(random.NextDouble() * 5):F2}%",
                        Change = $"{(random.NextDouble() * 2 - 1):+0.00;-0.00;0.00}%",
                        Impact = isPositive ? "Positive" : "Negative"
                    });
                    break;
                    
                case "Real GDP":
                    indicators.Add(new MarketIndicator
                    {
                        Key = name,
                        Value = $"{(random.NextDouble() * 4 - 1):F1}%",
                        Change = $"{(random.NextDouble() * 1 - 0.5):+0.00;-0.00;0.00}%",
                        Impact = isPositive ? "Positive" : "Negative"
                    });
                    break;
                    
                case "Consumer Confidence":
                    indicators.Add(new MarketIndicator
                    {
                        Key = name,
                        Value = $"{70 + random.NextDouble() * 30:F1}",
                        Change = $"{(random.NextDouble() * 4 - 2):+0.00;-0.00;0.00}",
                        Impact = isPositive ? "Positive" : "Negative"
                    });
                    break;
                    
                case "Unemployment Rate":
                    indicators.Add(new MarketIndicator
                    {
                        Key = name,
                        Value = $"{3 + random.NextDouble() * 7:F1}%",
                        Change = $"{(random.NextDouble() * 1 - 0.5):+0.00;-0.00;0.00}%",
                        Impact = random.NextDouble() < 0.7 ? "Negative" : "Positive" // Lower is better for unemployment
                    });
                    break;
            }
        }
        
        private string GetRandomWeatherCondition(Random random)
        {
            string[] conditions = { "Sunny", "Cloudy", "Rainy", "Snowy", "Partially Cloudy" };
            return conditions[random.Next(conditions.Length)];
        }
    }
}