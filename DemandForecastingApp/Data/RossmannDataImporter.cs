using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;
using DemandForecastingApp.Services;

namespace DemandForecastingApp.Data
{
    public class RossmannDataImporter
    {
        private const string TRAIN_FILE = "train.csv";
        private const string TEST_FILE = "test.csv";
        private const string STORE_FILE = "store.csv";
        
        private List<RossmannSalesRecord> _salesRecords;
        private List<RossmannStoreRecord> _storeRecords;
        
        public List<RossmannSalesRecord> SalesRecords => _salesRecords;
        public List<RossmannStoreRecord> StoreRecords => _storeRecords;
        
        public RossmannDataImporter()
        {
            _salesRecords = new List<RossmannSalesRecord>();
            _storeRecords = new List<RossmannStoreRecord>();
        }
        
        public List<RossmannSalesRecord> ImportData()
        {
            try
            {
                Logger.LogInfo("Starting Rossmann data import from Data folder");
                
                // Use DataService to get the Data folder path
                string dataFolderPath = DataService.GetDataFolderPath();
                
                Logger.LogInfo($"Using Data folder: {dataFolderPath}");
                
                // Verify that all required files exist
                if (!DataService.VerifyRossmannDataFiles(dataFolderPath))
                {
                    throw new FileNotFoundException("One or more required Rossmann data files are missing");
                }
                
                // Import store data first
                ImportStoreData(Path.Combine(dataFolderPath, STORE_FILE));
                
                // Import sales data
                ImportSalesData(Path.Combine(dataFolderPath, TRAIN_FILE));
                
                // Merge data
                MergeStoreAndSalesData();
                
                Logger.LogInfo($"Successfully imported {_salesRecords.Count} sales records and {_storeRecords.Count} store records");
                return _salesRecords;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error importing Rossmann data", ex);
                throw;
            }
        }
        
        // Overload to allow specifying a custom path
        public List<RossmannSalesRecord> ImportData(string dataFolderPath)
        {
            try
            {
                Logger.LogInfo($"Starting Rossmann data import from: {dataFolderPath}");
                
                if (!Directory.Exists(dataFolderPath))
                {
                    throw new DirectoryNotFoundException($"Specified data folder not found: {dataFolderPath}");
                }
                
                // Import store data first
                ImportStoreData(Path.Combine(dataFolderPath, STORE_FILE));
                
                // Import sales data
                ImportSalesData(Path.Combine(dataFolderPath, TRAIN_FILE));
                
                // Merge data
                MergeStoreAndSalesData();
                
                Logger.LogInfo($"Successfully imported {_salesRecords.Count} sales records and {_storeRecords.Count} store records");
                return _salesRecords;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error importing Rossmann data", ex);
                throw;
            }
        }
        
        public static List<StoreRecord> ImportStoreData(string storeFilePath)
        {
            var storeData = new List<StoreRecord>();

            try
            {
                Console.WriteLine($"Attempting to load store data from: {storeFilePath}");

                if (!File.Exists(storeFilePath))
                {
                    Console.WriteLine($"File not found: {storeFilePath}");
                    return storeData;
                }

                var lines = File.ReadAllLines(storeFilePath);
                if (lines.Length <= 1)
                {
                    Console.WriteLine("The file is empty or contains only headers.");
                    return storeData;
                }

                for (int i = 1; i < lines.Length; i++) // Skip the header row
                {
                    var line = lines[i];
                    var columns = line.Split(',');

                    if (columns.Length < 10) // Ensure there are enough columns
                    {
                        Console.WriteLine($"Skipping invalid or incomplete line {i + 1}: {line}");
                        continue;
                    }

                    try
                    {
                        var record = new StoreRecord
                        {
                            StoreId = int.Parse(columns[0]),
                            StoreType = columns[1],
                            Assortment = columns[2],
                            CompetitionDistance = string.IsNullOrWhiteSpace(columns[3]) ? (double?)null : double.Parse(columns[3]),
                            CompetitionOpenSinceMonth = string.IsNullOrWhiteSpace(columns[4]) ? (int?)null : int.Parse(columns[4]),
                            CompetitionOpenSinceYear = string.IsNullOrWhiteSpace(columns[5]) ? (int?)null : int.Parse(columns[5]),
                            Promo2 = columns[6] == "1",
                            Promo2SinceWeek = string.IsNullOrWhiteSpace(columns[7]) ? (int?)null : int.Parse(columns[7]),
                            Promo2SinceYear = string.IsNullOrWhiteSpace(columns[8]) ? (int?)null : int.Parse(columns[8]),
                            PromoInterval = columns[9]
                        };
                        storeData.Add(record);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line {i + 1}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully loaded {storeData.Count} store records.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading store data: {ex.Message}");
            }

            return storeData;
        }

        public static List<DemandRecord> ImportSalesData(string salesFilePath)
        {
            var salesData = new List<DemandRecord>();

            try
            {
                // Log the file path being used
                Console.WriteLine($"Attempting to load sales data from: {salesFilePath}");

                if (!File.Exists(salesFilePath))
                {
                    Console.WriteLine($"File not found: {salesFilePath}");
                    return salesData;
                }

                var lines = File.ReadAllLines(salesFilePath);
                if (lines.Length <= 1)
                {
                    Console.WriteLine("The file is empty or contains only headers.");
                    return salesData;
                }

                // Parse the CSV file
                for (int i = 1; i < lines.Length; i++) // Skip the header row
                {
                    var line = lines[i];
                    var columns = line.Split(',');

                    if (columns.Length < 9) // Ensure there are enough columns
                    {
                        Console.WriteLine($"Skipping invalid or incomplete line {i + 1}: {line}");
                        continue;
                    }

                    try
                    {
                        var record = new DemandRecord
                        {
                            StoreId = int.Parse(columns[0]),
                            Date = DateTime.Parse(columns[2]),
                            Sales = double.Parse(columns[3]),
                            Customers = int.Parse(columns[4]),
                            Open = int.Parse(columns[5]) == 1,
                            Promo = int.Parse(columns[6]) == 1,
                            StateHoliday = columns[7],
                            SchoolHoliday = int.Parse(columns[8]) == 1
                        };
                        salesData.Add(record);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line {i + 1}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully loaded {salesData.Count} sales records.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sales data: {ex.Message}");
            }

            return salesData;
        }

        public static List<DemandRecord> ImportTestData(string testFilePath)
        {
            var testData = new List<DemandRecord>();

            try
            {
                Console.WriteLine($"Attempting to load test data from: {testFilePath}");

                if (!File.Exists(testFilePath))
                {
                    Console.WriteLine($"File not found: {testFilePath}");
                    return testData;
                }

                var lines = File.ReadAllLines(testFilePath);
                if (lines.Length <= 1)
                {
                    Console.WriteLine("The file is empty or contains only headers.");
                    return testData;
                }

                for (int i = 1; i < lines.Length; i++) // Skip the header row
                {
                    var line = lines[i];
                    var columns = line.Split(',');

                    if (columns.Length < 8) // Ensure there are enough columns
                    {
                        Console.WriteLine($"Skipping invalid or incomplete line {i + 1}: {line}");
                        continue;
                    }

                    try
                    {
                        var record = new DemandRecord
                        {
                            StoreId = int.Parse(columns[1]),
                            Date = DateTime.Parse(columns[3]),
                            Open = columns[4] == "1",
                            Promo = columns[5] == "1",
                            StateHoliday = columns[6],
                            SchoolHoliday = columns[7] == "1"
                        };
                        testData.Add(record);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line {i + 1}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully loaded {testData.Count} test records.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading test data: {ex.Message}");
            }

            return testData;
        }
        
        private void MergeStoreAndSalesData()
        {
            foreach (var salesRecord in _salesRecords)
            {
                var storeRecord = _storeRecords.FirstOrDefault(s => s.StoreId == salesRecord.StoreId);
                if (storeRecord != null)
                {
                    salesRecord.StoreType = storeRecord.StoreType;
                    salesRecord.Assortment = storeRecord.Assortment;
                    salesRecord.CompetitionDistance = storeRecord.CompetitionDistance;
                    salesRecord.CompetitionOpenSinceMonth = storeRecord.CompetitionOpenSinceMonth;
                    salesRecord.CompetitionOpenSinceYear = storeRecord.CompetitionOpenSinceYear;
                    salesRecord.Promo2 = storeRecord.Promo2;
                    salesRecord.Promo2SinceWeek = storeRecord.Promo2SinceWeek;
                    salesRecord.Promo2SinceYear = storeRecord.Promo2SinceYear;
                    salesRecord.PromoInterval = storeRecord.PromoInterval;
                }
            }
        }
        
        public List<RossmannSalesRecord> FeatureEngineering(List<RossmannSalesRecord> records)
        {
            foreach (var record in records)
            {
                // Extract date features
                record.Year = record.Date.Year;
                record.Month = record.Date.Month;
                record.Day = record.Date.Day;
                record.DayOfWeek = (int)record.Date.DayOfWeek;
                record.IsWeekend = record.DayOfWeek == 0 || record.DayOfWeek == 6;
                
                // Process holidays
                record.IsPublicHoliday = record.StateHoliday == "a";
                record.IsEasterHoliday = record.StateHoliday == "b";
                record.IsChristmas = record.StateHoliday == "c";
                
                // Process competition features
                if (record.CompetitionOpenSinceYear.HasValue && record.CompetitionOpenSinceMonth.HasValue)
                {
                    var competitionOpenDate = new DateTime(
                        record.CompetitionOpenSinceYear.Value,
                        record.CompetitionOpenSinceMonth.Value, 
                        1);
                    
                    record.CompetitionOpenMonths = 
                        (record.Date.Year - competitionOpenDate.Year) * 12 + 
                        record.Date.Month - competitionOpenDate.Month;
                }
                else
                {
                    record.CompetitionOpenMonths = 0; // No competition
                }
                
                // Process promo features
                if (record.Promo2 == 1 && record.Promo2SinceYear.HasValue && record.Promo2SinceWeek.HasValue)
                {
                    // Create a DateTime for the start of Promo2
                    var promo2StartDate = GetDateFromWeekAndYear(
                        record.Promo2SinceYear.Value, 
                        record.Promo2SinceWeek.Value);
                    
                    // Calculate months since Promo2 started
                    record.Promo2ActiveMonths = 
                        (record.Date.Year - promo2StartDate.Year) * 12 + 
                        record.Date.Month - promo2StartDate.Month;
                    
                    // Check if current month is in PromoInterval
                    if (!string.IsNullOrEmpty(record.PromoInterval))
                    {
                        var months = record.PromoInterval.Split(',');
                        var currentMonthName = record.Date.ToString("MMM", CultureInfo.InvariantCulture);
                        record.Promo2Active = months.Any(m => 
                            string.Equals(m.Trim(), currentMonthName, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        record.Promo2Active = false;
                    }
                }
                else
                {
                    record.Promo2ActiveMonths = 0;
                    record.Promo2Active = false;
                }
            }
            
            return records;
        }
        
        private DateTime GetDateFromWeekAndYear(int year, int weekNumber)
        {
            // Get the first day of the year
            var firstDay = new DateTime(year, 1, 1);
            
            // Get the first day of week 1
            var daysOffset = DayOfWeek.Monday - firstDay.DayOfWeek;
            if (daysOffset > 0) daysOffset -= 7; // Adjust if first day is already after Monday
            var firstMonday = firstDay.AddDays(daysOffset);
            
            // Add the required number of weeks
            var result = firstMonday.AddDays((weekNumber - 1) * 7);
            return result;
        }
        
        private int? ParseNullableInt(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "NA")
                return null;
            
            return int.TryParse(value, out int result) ? result : (int?)null;
        }
        
        private float? ParseNullableFloat(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "NA")
                return null;
            
            return float.TryParse(value, out float result) ? result : (float?)null;
        }
    }
}