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
    public class DataImporter
    {
        public string ProductId { get; set; } = "DefaultProduct";
        private readonly string[] _validDateFormats = { "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };
        
        public List<DemandRecord> ImportCsvData(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");
            }
            
            try
            {
                Logger.LogInfo($"Importing CSV data from: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The specified file does not exist: {filePath}");
                }
                
                // Validate file extension
                if (!Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("The specified file is not a CSV file");
                }

                var records = new List<DemandRecord>();
                int lineNumber = 0;
                
                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    
                    // Read and validate header
                    string[] headers = parser.ReadFields();
                    lineNumber++;
                    
                    if (headers == null || headers.Length == 0)
                    {
                        throw new FormatException("CSV file is empty or has invalid format");
                    }
                    
                    int dateIndex = Array.FindIndex(headers, h => h.Equals("Date", StringComparison.OrdinalIgnoreCase));
                    int salesIndex = Array.FindIndex(headers, h => h.Equals("Sales", StringComparison.OrdinalIgnoreCase));
                    
                    if (dateIndex < 0 || salesIndex < 0)
                    {
                        throw new FormatException("CSV file must contain 'Date' and 'Sales' columns");
                    }
                    
                    // Find optional columns - case insensitive search
                    int holidayIndex = Array.FindIndex(headers, h => h.Equals("Holiday", StringComparison.OrdinalIgnoreCase) 
                                                    || h.Equals("StateHoliday", StringComparison.OrdinalIgnoreCase));
                    
                    // Parse data rows
                    while (!parser.EndOfData)
                    {
                        try
                        {
                            string[] fields = parser.ReadFields();
                            lineNumber++;
                            
                            if (fields == null || fields.Length <= Math.Max(dateIndex, salesIndex))
                            {
                                Logger.LogWarning($"Skipping line {lineNumber}: Insufficient fields");
                                continue;
                            }
                            
                            // Parse date field using multiple formats
                            if (!TryParseDate(fields[dateIndex], out DateTime date))
                            {
                                Logger.LogWarning($"Skipping line {lineNumber}: Invalid date format '{fields[dateIndex]}'");
                                continue;
                            }
                            
                            // Parse sales value
                            if (!float.TryParse(fields[salesIndex], out float sales))
                            {
                                Logger.LogWarning($"Skipping line {lineNumber}: Invalid sales value '{fields[salesIndex]}'");
                                continue;
                            }
                            
                            var record = new DemandRecord
                            {
                                Date = date,
                                Sales = sales
                            };
                            
                            // Add optional fields if present
                            if (holidayIndex >= 0 && fields.Length > holidayIndex)
                            {
                                record.StateHoliday = string.IsNullOrEmpty(fields[holidayIndex]) ? "0" : fields[holidayIndex];
                            }
                            
                            records.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"Error parsing record at line {lineNumber}: {ex.Message}");
                        }
                    }
                }
                
                if (records.Count == 0)
                {
                    Logger.LogWarning("No valid records were imported from the CSV file");
                }
                else
                {
                    Logger.LogInfo($"Successfully imported {records.Count} records from CSV");
                    
                    // Save a copy of the imported data
                    try
                    {
                        string importedDataFolder = DataService.GetDataDirectoryForProduct(ProductId);
                        string backupFileName = $"imported_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        File.Copy(filePath, Path.Combine(importedDataFolder, backupFileName));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to create backup of imported data: {ex.Message}");
                    }
                }
                
                return records;
            }
            catch (IOException ex)
            {
                Logger.LogError($"File access error while importing CSV data: {ex.Message}", ex);
                throw new IOException($"Unable to access the file: {ex.Message}", ex);
            }
            catch (FormatException ex)
            {
                Logger.LogError($"Format error while importing CSV data: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Unexpected error importing CSV data: {ex.Message}", ex);
                throw;
            }
        }
        
        private bool TryParseDate(string dateString, out DateTime result)
        {
            return DateTime.TryParseExact(dateString, _validDateFormats, 
                                         CultureInfo.InvariantCulture, 
                                         DateTimeStyles.None, out result) ||
                   DateTime.TryParse(dateString, out result);
        }
        
        public List<DemandRecord> ImportExcelData(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");
            }
            
            try
            {
                Logger.LogInfo($"Importing Excel data from: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The specified file does not exist: {filePath}");
                }
                
                // Validate file extension
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension != ".xls" && extension != ".xlsx")
                {
                    throw new ArgumentException("The specified file is not an Excel file");
                }
                
                // This is a stub implementation - in a real application, you would use a library
                // like EPPlus, NPOI, or ExcelDataReader to read Excel files
                
                throw new NotImplementedException("Excel import is not implemented in this demo version. Please use CSV import instead.");
            }
            catch (IOException ex)
            {
                Logger.LogError($"File access error while importing Excel data: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing Excel data: {ex.Message}", ex);
                throw;
            }
        }
        
        public List<DemandRecord> GenerateDemoData(int count = 100)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Record count must be greater than zero", nameof(count));
            }
            
            try
            {
                Logger.LogInfo($"Generating {count} demo records");
                
                var records = new List<DemandRecord>();
                var random = new Random(42); // Fixed seed for reproducibility
                
                DateTime startDate = DateTime.Today.AddDays(-count);
                
                // Generate some realistic-looking demand data with weekly patterns
                for (int i = 0; i < count; i++)
                {
                    var date = startDate.AddDays(i);
                    
                    // Base demand varies by day of week
                    float baseDemand = date.DayOfWeek switch
                    {
                        DayOfWeek.Monday => 100,
                        DayOfWeek.Tuesday => 110,
                        DayOfWeek.Wednesday => 120,
                        DayOfWeek.Thursday => 115,
                        DayOfWeek.Friday => 130,
                        DayOfWeek.Saturday => 150,
                        DayOfWeek.Sunday => 90,
                        _ => 100
                    };
                    
                    // Add trend component (slight upward trend)
                    float trendComponent = i * 0.5f;
                    
                    // Add random noise
                    float noise = (float)(random.NextDouble() * 20 - 10);
                    
                    // Holiday effects
                    string holiday = "0";
                    float holidayEffect = 0;
                    
                    // Simulate some holidays
                    if (date.Month == 1 && date.Day == 1)
                    {
                        // New Year's Day
                        holiday = "a";
                        holidayEffect = -30;
                    }
                    else if (date.Month == 7 && date.Day == 4)
                    {
                        // Independence Day
                        holiday = "b";
                        holidayEffect = 20;
                    }
                    else if (date.Month == 12 && date.Day == 25)
                    {
                        // Christmas
                        holiday = "c";
                        holidayEffect = 50;
                    }
                    
                    // Calculate final demand
                    float sales = Math.Max(0, baseDemand + trendComponent + noise + holidayEffect);
                    
                    records.Add(new DemandRecord
                    {
                        Date = date,
                        Sales = sales,
                        StateHoliday = holiday
                    });
                }
                
                // Save demo data to file
                try
                {
                    string dataFolder = DataService.GetDataDirectoryForProduct(ProductId);
                    string demoFileName = $"demo_data_{DateTime.Now:yyyyMMdd}.csv";
                    string filePath = Path.Combine(dataFolder, demoFileName);
                    
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        // Write header
                        writer.WriteLine("Date,Sales,StateHoliday");
                        
                        // Write data
                        foreach (var record in records)
                        {
                            writer.WriteLine($"{record.Date:yyyy-MM-dd},{record.Sales},{record.StateHoliday}");
                        }
                    }
                    
                    Logger.LogInfo($"Demo data saved to {filePath}");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to save demo data to file: {ex.Message}");
                }
                
                Logger.LogInfo($"Generated {records.Count} demo records");
                return records;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error generating demo data: {ex.Message}", ex);
                throw;
            }
        }
    }
}

