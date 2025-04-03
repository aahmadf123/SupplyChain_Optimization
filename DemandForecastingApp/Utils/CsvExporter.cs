using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace DemandForecastingApp.Utils
{
    public class CsvExporter
    {
        public bool ExportData<T>(IEnumerable<T> data, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(data);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting CSV: {ex.Message}");
                return false;
            }
        }
        
        public bool ExportForecastData(IEnumerable<ForecastDataPoint> forecastData, string filePath)
        {
            return ExportData(forecastData, filePath);
        }
        
        public bool ExportInventoryRecommendations(IEnumerable<InventoryRecommendation> recommendations, string filePath)
        {
            return ExportData(recommendations, filePath);
        }
    }
    
    // These classes need to be accessible here for CSV export
    public class ForecastDataPoint
    {
        public string Period { get; set; }
        public double ForecastedDemand { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public string ReorderPoint { get; set; }
    }
    
    public class InventoryRecommendation
    {
        public string Item { get; set; }
        public int CurrentStock { get; set; }
        public int RecommendedOrder { get; set; }
    }
}