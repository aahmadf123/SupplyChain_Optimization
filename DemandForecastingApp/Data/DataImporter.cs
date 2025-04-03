using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace DemandForecastingApp.Data
{
    // Define a record representing one row of data.
    public class DemandRecord
    {
        public DateTime Date { get; set; }
        public float Demand { get; set; }
    }

    // Map CSV columns to the DemandRecord properties.
    public sealed class DemandRecordMap : ClassMap<DemandRecord>
    {
        public DemandRecordMap()
        {
            Map(m => m.Date).Name("Date");
            Map(m => m.Demand).Name("Demand");
        }
    }

    public class DataImporter
    {
        // Imports CSV data and returns a list of DemandRecord.
        public List<DemandRecord> ImportCsv(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<DemandRecordMap>();
                var records = new List<DemandRecord>(csv.GetRecords<DemandRecord>());
                return records;
            }
        }

        // Cleans the data by filtering out invalid records.
        public List<DemandRecord> CleanData(List<DemandRecord> records)
        {
            // Example: Remove records with null dates or negative demand values.
            return records.FindAll(record => record.Date != default && record.Demand >= 0);
        }

        // Performs basic EDA by calculating and displaying summary statistics.
        public void PerformEDA(List<DemandRecord> records)
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No records available for analysis.");
                return;
            }

            int count = records.Count;
            double totalDemand = 0.0;
            float minDemand = float.MaxValue;
            float maxDemand = float.MinValue;

            foreach (var record in records)
            {
                totalDemand += record.Demand;
                if (record.Demand < minDemand) minDemand = record.Demand;
                if (record.Demand > maxDemand) maxDemand = record.Demand;
            }

            double avgDemand = totalDemand / count;

            Console.WriteLine($"Total Records: {count}");
            Console.WriteLine($"Average Demand: {avgDemand:F2}");
            Console.WriteLine($"Minimum Demand: {minDemand}");
            Console.WriteLine($"Maximum Demand: {maxDemand}");
        }
    }
}
