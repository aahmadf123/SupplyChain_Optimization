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
        
        private void ImportStoreData(string storeFilePath)
        {
            if (!File.Exists(storeFilePath))
            {
                throw new FileNotFoundException($"Store data file not found: {storeFilePath}");
            }
            
            _storeRecords.Clear();
            var lines = File.ReadAllLines(storeFilePath);
            
            // Skip header
            for (int i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(',');
                if (fields.Length < 10) continue; // Skip invalid lines
                
                try
                {
                    var store = new RossmannStoreRecord
                    {
                        StoreId = int.Parse(fields[0]),
                        StoreType = fields[1],
                        Assortment = fields[2],
                        CompetitionDistance = ParseNullableInt(fields[3]),
                        CompetitionOpenSinceMonth = ParseNullableInt(fields[4]),
                        CompetitionOpenSinceYear = ParseNullableInt(fields[5]),
                        Promo2 = int.Parse(fields[6]),
                        Promo2SinceWeek = ParseNullableInt(fields[7]),
                        Promo2SinceYear = ParseNullableInt(fields[8]),
                        PromoInterval = fields[9]
                    };
                    
                    _storeRecords.Add(store);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error parsing store record at line {i+1}: {ex.Message}");
                }
            }
            
            Logger.LogInfo($"Imported {_storeRecords.Count} store records from {storeFilePath}");
        }
        
        private void ImportSalesData(string salesFilePath)
        {
            if (!File.Exists(salesFilePath))
            {
                throw new FileNotFoundException($"Sales data file not found: {salesFilePath}");
            }
            
            _salesRecords.Clear();
            var lines = File.ReadAllLines(salesFilePath);
            
            // For large files, read only a subset to avoid memory issues
            const int MAX_RECORDS = 50000;  // Adjust based on your system capabilities
            
            // Skip header
            for (int i = 1; i < lines.Length && (_salesRecords.Count < MAX_RECORDS); i++)
            {
                var fields = lines[i].Split(',');
                if (fields.Length < 9) continue; // Skip invalid lines
                
                try
                {
                    var record = new RossmannSalesRecord
                    {
                        StoreId = int.Parse(fields[0]),
                        Date = DateTime.ParseExact(fields[2], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Sales = ParseNullableFloat(fields[3]),
                        Customers = ParseNullableInt(fields[4]),
                        Open = int.Parse(fields[5]),
                        StateHoliday = fields[6],
                        SchoolHoliday = int.Parse(fields[7]),
                        Promo = int.Parse(fields[8])
                    };
                    
                    _salesRecords.Add(record);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error parsing sales record at line {i+1}: {ex.Message}");
                }
            }
            
            Logger.LogInfo($"Imported {_salesRecords.Count} sales records from {salesFilePath} (limited to {MAX_RECORDS})");
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