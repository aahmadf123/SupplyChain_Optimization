using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using DemandForecastingApp.Models;

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
                Logger.LogInfo($"Successfully exported data to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error exporting CSV to {filePath}", ex);
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
}