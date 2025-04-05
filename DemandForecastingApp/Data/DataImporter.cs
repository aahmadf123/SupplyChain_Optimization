using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Data
{
    public class DataImporter
    {
        public string ProductId { get; set; }
        
        public List<DemandRecord> ImportCsvData(string filePath)
        {
            try
            {
                Logger.LogInfo($"Importing CSV data from: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The specified file does not exist: {filePath}");
                }
                
                var records = new List<DemandRecord>();
                
                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    
                    // Read and validate header
                    string[] headers = parser.ReadFields();
                    int dateIndex = Array.IndexOf(headers, "Date");
                    int salesIndex = Array.IndexOf(headers, "Sales");
                    
                    if (dateIndex < 0 || salesIndex < 0)
                    {
                        throw new FormatException("CSV file must contain 'Date' and 'Sales' columns");
                    }
                    
                    // Find optional columns
                    int holidayIndex = Array.IndexOf(headers, "Holiday");
                    
                    // Parse data rows
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        try
                        {
                            var record = new DemandRecord
                            {
                                Date = DateTime.Parse(fields[dateIndex]),
                                Sales = float.Parse(fields[salesIndex])
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
                            Logger.LogWarning($"Error parsing record: {ex.Message}");
                        }
                    }
                }
                
                Logger.LogInfo($"Successfully imported {records.Count} records from CSV");
                return records;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing CSV data: {ex.Message}", ex);
                throw;
            }
        }
        
        public List<DemandRecord> ImportExcelData(string filePath)
        {
            try
            {
                Logger.LogInfo($"Importing Excel data from: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The specified file does not exist: {filePath}");
                }
                
                // This is a stub implementation - in a real application, you would use a library
                // like EPPlus, NPOI, or ExcelDataReader to read Excel files
                
                throw new NotImplementedException("Excel import is not implemented in this demo version");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing Excel data: {ex.Message}", ex);
                throw;
            }
        }
        
        public List<DemandRecord> GenerateDemoData(int count = 100)
        {
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

