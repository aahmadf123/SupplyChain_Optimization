using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Data
{
    public class Record
    {
        public DateTime Date { get; set; }
        public double Demand { get; set; }
        public string ProductId { get; set; }
    }

    public class DataImporter
    {
        public List<Record> Records { get; private set; }

        public DataImporter()
        {
            Records = new List<Record>();
        }

        public List<Record> ImportCsv(string filePath)
        {
            try
            {
                // Reset records
                Records = new List<Record>();
                
                // Read all lines from the CSV file
                var lines = File.ReadAllLines(filePath);
                
                // Skip the header and parse each line
                foreach (var line in lines.Skip(1)) 
                {
                    var values = line.Split(',');
                    
                    // Validate that the line has enough columns
                    if (values.Length >= 3)
                    {
                        if (DateTime.TryParse(values[0], out DateTime date) && 
                            double.TryParse(values[1], out double demand) &&
                            values[2] is string productId)
                        {
                            Records.Add(new Record
                            {
                                Date = date,
                                Demand = demand,
                                ProductId = productId
                            });
                        }
                    }
                }
                
                Logger.LogInfo($"Imported {Records.Count} records from {filePath}");
                return Records;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error importing CSV file: {filePath}", ex);
                throw;
            }
        }

        public List<Record> CleanData(List<Record> data)
        {
            try
            {
                // Remove records with zero or negative demand
                var cleanedData = data.Where(r => r.Demand > 0).ToList();
                
                // Sort by date
                cleanedData = cleanedData.OrderBy(r => r.Date).ToList();
                
                Logger.LogInfo($"Cleaned data: Removed {data.Count - cleanedData.Count} invalid records");
                
                // Update stored records
                Records = cleanedData;
                return cleanedData;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error cleaning data", ex);
                throw;
            }
        }

        public void PerformEDA(List<Record> data)
        {
            try
            {
                // Calculate basic statistics
                double totalDemand = data.Sum(r => r.Demand);
                double avgDemand = data.Average(r => r.Demand);
                double maxDemand = data.Max(r => r.Demand);
                double minDemand = data.Min(r => r.Demand);
                
                // Get date range
                DateTime minDate = data.Min(r => r.Date);
                DateTime maxDate = data.Max(r => r.Date);
                
                // Get unique product count
                int productCount = data.Select(r => r.ProductId).Distinct().Count();
                
                // Log results
                Logger.LogInfo("Exploratory Data Analysis Results:");
                Logger.LogInfo($"Date Range: {minDate:d} to {maxDate:d}");
                Logger.LogInfo($"Total Records: {data.Count}");
                Logger.LogInfo($"Unique Products: {productCount}");
                Logger.LogInfo($"Total Demand: {totalDemand:F2}");
                Logger.LogInfo($"Average Demand: {avgDemand:F2}");
                Logger.LogInfo($"Min Demand: {minDemand:F2}");
                Logger.LogInfo($"Max Demand: {maxDemand:F2}");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error performing EDA", ex);
            }
        }
    }
}
