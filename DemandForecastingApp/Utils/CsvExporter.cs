using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace DemandForecastingApp.Utils
{
    public static class CsvExporter
    {
        public static void ExportToCsv<T>(IEnumerable<T> records, string filePath)
        {
            try
            {
                using var writer = new StreamWriter(filePath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                
                csv.WriteRecords(records);
                
                Logger.LogInfo($"Data exported to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error exporting data to CSV: {ex.Message}", ex);
                throw;
            }
        }
    }
}