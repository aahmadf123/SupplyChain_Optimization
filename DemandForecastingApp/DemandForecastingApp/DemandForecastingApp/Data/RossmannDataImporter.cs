using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
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
        
        private readonly List<RossmannSalesRecord> _salesRecords;
        private readonly List<RossmannStoreRecord> _storeRecords;
        private string _dataFolderPath;
        
        public List<RossmannSalesRecord> SalesRecords => _salesRecords;
        public List<RossmannStoreRecord> StoreRecords => _storeRecords;
        
        public RossmannDataImporter(string dataFolderPath = null)
        {
            _salesRecords = new List<RossmannSalesRecord>();
            _storeRecords = new List<RossmannStoreRecord>();
            _dataFolderPath = dataFolderPath;
        }
        
        public List<RossmannSalesRecord> ImportSalesData(string dataFolderPath)
        {
            _dataFolderPath = dataFolderPath;
            return ImportData(dataFolderPath);
        }

        public List<RossmannStoreRecord> ImportStoreData(string dataFolderPath)
        {
            _dataFolderPath = dataFolderPath;
            ImportData(dataFolderPath);
            return _storeRecords;
        }

        public List<RossmannSalesRecord> ImportTestData(string dataFolderPath)
        {
            _dataFolderPath = dataFolderPath;
            try
            {
                Logger.LogInfo($"Starting Rossmann test data import from: {dataFolderPath}");
                
                if (!Directory.Exists(dataFolderPath))
                {
                    throw new DirectoryNotFoundException($"Specified data folder not found: {dataFolderPath}");
                }
                
                _salesRecords.Clear();
                
                string testFilePath = Path.Combine(dataFolderPath, TEST_FILE);
                Logger.LogInfo($"Loading test data from: {testFilePath}");
                
                using (TextFieldParser parser = new TextFieldParser(testFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    
                    parser.ReadLine(); // Skip header
                    
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        try
                        {
                            var record = new RossmannSalesRecord
                            {
                                StoreId = int.Parse(fields[0]),
                                Date = DateTime.Parse(fields[2]),
                                Sales = 0,
                                Customers = 0,
                                Open = int.Parse(fields[3]),
                                Promo = int.Parse(fields[4]),
                                StateHoliday = string.IsNullOrEmpty(fields[5]) ? "0" : fields[5],
                                SchoolHoliday = int.Parse(fields[6]),
                                StoreType = "Unknown",
                                Assortment = "Unknown",
                                PromoInterval = "None"
                            };
                            _salesRecords.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Error parsing test record: {ex.Message}");
                        }
                    }
                }
                
                if (_storeRecords.Count == 0)
                {
                    ImportStoreData(dataFolderPath);
                }
                MergeStoreAndSalesData();
                
                return _salesRecords;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing Rossmann test data: {ex.Message}");
                throw;
            }
        }

        public void FeatureEngineering(string options = null)
        {
            foreach (var record in _salesRecords)
            {
                // Extract temporal features
                record.Year = record.Date.Year;
                record.Month = record.Date.Month;
                record.Day = record.Date.Day;
                record.DayOfWeek = (int)record.Date.DayOfWeek == 0 ? 7 : (int)record.Date.DayOfWeek;
                record.IsWeekend = record.DayOfWeek >= 6;
                
                // Holiday features
                record.IsPublicHoliday = record.StateHoliday != "0";
                record.IsEasterHoliday = record.StateHoliday == "a";
                record.IsChristmas = record.StateHoliday == "b";
                
                // Competition features
                if (record.CompetitionOpenSinceYear.HasValue && record.CompetitionOpenSinceMonth.HasValue)
                {
                    var competitionOpenDate = new DateTime(
                        record.CompetitionOpenSinceYear.Value,
                        record.CompetitionOpenSinceMonth.Value,
                        1);
                    record.CompetitionOpenMonths = ((record.Date.Year - competitionOpenDate.Year) * 12) +
                        record.Date.Month - competitionOpenDate.Month;
                }
                else
                {
                    record.CompetitionOpenMonths = 0;
                }
                
                // Promo2 features
                if (record.Promo2SinceYear.HasValue && record.Promo2SinceWeek.HasValue)
                {
                    var promo2StartDate = ISOWeek.ToDateTime(
                        record.Promo2SinceYear.Value,
                        record.Promo2SinceWeek.Value,
                        DayOfWeek.Monday);
                    record.Promo2ActiveMonths = (int)(record.Date - promo2StartDate).TotalDays / 30;
                    
                    if (record.PromoInterval != "None")
                    {
                        var months = record.PromoInterval.Split(',');
                        record.Promo2Active = months.Contains(
                            CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(record.Date.Month)
                        );
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
        }
        
        public List<RossmannSalesRecord> ImportData(string dataFolderPath)
        {
            try
            {
                _dataFolderPath = dataFolderPath;
                Logger.LogInfo($"Starting Rossmann data import from: {dataFolderPath}");
                
                if (!Directory.Exists(dataFolderPath))
                {
                    throw new DirectoryNotFoundException($"Specified data folder not found: {dataFolderPath}");
                }
                
                _salesRecords.Clear();
                _storeRecords.Clear();
                
                string storeFilePath = Path.Combine(dataFolderPath, STORE_FILE);
                Logger.LogInfo($"Loading store data from: {storeFilePath}");
                
                using (TextFieldParser parser = new TextFieldParser(storeFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    
                    parser.ReadLine();
                    
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        try
                        {
                            var record = new RossmannStoreRecord
                            {
                                StoreId = int.Parse(fields[0]),
                                StoreType = string.IsNullOrEmpty(fields[1]) ? "Unknown" : fields[1],
                                Assortment = string.IsNullOrEmpty(fields[2]) ? "Unknown" : fields[2],
                                CompetitionDistance = string.IsNullOrEmpty(fields[3]) ? null : int.Parse(fields[3]),
                                CompetitionOpenSinceMonth = string.IsNullOrEmpty(fields[4]) ? null : int.Parse(fields[4]),
                                CompetitionOpenSinceYear = string.IsNullOrEmpty(fields[5]) ? null : int.Parse(fields[5]),
                                Promo2 = fields[6] == "1" ? 1 : 0,
                                Promo2SinceWeek = string.IsNullOrEmpty(fields[7]) ? null : int.Parse(fields[7]),
                                Promo2SinceYear = string.IsNullOrEmpty(fields[8]) ? null : int.Parse(fields[8]),
                                PromoInterval = string.IsNullOrEmpty(fields[9]) ? "None" : fields[9]
                            };
                            _storeRecords.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Error parsing store record: {ex.Message}");
                        }
                    }
                }
                
                Logger.LogInfo($"Successfully loaded {_storeRecords.Count} store records");
                
                string salesFilePath = Path.Combine(dataFolderPath, TRAIN_FILE);
                Logger.LogInfo($"Loading sales data from: {salesFilePath}");
                
                using (TextFieldParser parser = new TextFieldParser(salesFilePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    
                    parser.ReadLine();
                    
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        try
                        {
                            var record = new RossmannSalesRecord
                            {
                                StoreId = int.Parse(fields[0]),
                                Date = DateTime.Parse(fields[2]),
                                Sales = float.Parse(fields[3]),
                                Customers = int.Parse(fields[4]),
                                Open = int.Parse(fields[5]),
                                Promo = int.Parse(fields[6]),
                                StateHoliday = string.IsNullOrEmpty(fields[7]) ? "0" : fields[7],
                                SchoolHoliday = int.Parse(fields[8]),
                                StoreType = "Unknown",
                                Assortment = "Unknown",
                                PromoInterval = "None"
                            };
                            _salesRecords.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Error parsing sales record: {ex.Message}");
                        }
                    }
                }
                
                Logger.LogInfo($"Successfully loaded {_salesRecords.Count} sales records");
                
                MergeStoreAndSalesData();
                
                Logger.LogInfo($"Final count after merging: {_salesRecords.Count} sales records with store data");
                return _salesRecords;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing Rossmann data: {ex.Message}");
                throw;
            }
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
    }
}
