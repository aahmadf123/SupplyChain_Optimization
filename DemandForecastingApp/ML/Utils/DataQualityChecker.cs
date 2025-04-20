using System;
using System.Collections.Generic;
using System.Linq;
using DemandForecastingApp.Models;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.ML.Utils
{
    /// <summary>
    /// Validates data quality and detects anomalies in sales data
    /// </summary>
    public class DataQualityChecker
    {
        private readonly float _zScoreThreshold;
        private readonly int _minRequiredRecords;
        private readonly float _maxMissingPercentage;

        public DataQualityChecker(
            float zScoreThreshold = 3.0f,
            int minRequiredRecords = 30,
            float maxMissingPercentage = 0.1f)
        {
            _zScoreThreshold = zScoreThreshold;
            _minRequiredRecords = minRequiredRecords;
            _maxMissingPercentage = maxMissingPercentage;
        }

        /// <summary>
        /// Validates the quality of sales data
        /// </summary>
        /// <param name="data">Sales data to validate</param>
        /// <returns>Data quality report</returns>
        public DataQualityReport ValidateData(List<RossmannSalesRecord> data)
        {
            try
            {
                Logger.LogInfo($"Validating data quality for {data?.Count ?? 0} records");

                var report = new DataQualityReport
                {
                    TotalRecords = data?.Count ?? 0,
                    ValidationTime = DateTime.UtcNow
                };

                if (data == null || data.Count == 0)
                {
                    report.IsValid = false;
                    report.Issues.Add("No data provided");
                    return report;
                }

                // Check minimum required records
                if (data.Count < _minRequiredRecords)
                {
                    report.IsValid = false;
                    report.Issues.Add($"Insufficient data: {data.Count} records (minimum {_minRequiredRecords} required)");
                }

                // Check for missing values
                var missingValues = CheckMissingValues(data);
                report.MissingValues = missingValues;
                if (missingValues > 0)
                {
                    float missingPercentage = (float)missingValues / data.Count;
                    if (missingPercentage > _maxMissingPercentage)
                    {
                        report.IsValid = false;
                        report.Issues.Add($"Too many missing values: {missingPercentage:P2} (maximum {_maxMissingPercentage:P2} allowed)");
                    }
                }

                // Check for date continuity
                var dateGaps = CheckDateContinuity(data);
                report.DateGaps = dateGaps;
                if (dateGaps > 0)
                {
                    report.IsValid = false;
                    report.Issues.Add($"Found {dateGaps} gaps in date sequence");
                }

                // Check for anomalies
                var anomalies = DetectAnomalies(data);
                report.Anomalies = anomalies;
                if (anomalies.Count > 0)
                {
                    report.IsValid = false;
                    report.Issues.Add($"Found {anomalies.Count} anomalies in the data");
                }

                // Check value ranges
                var rangeIssues = CheckValueRanges(data);
                report.RangeIssues = rangeIssues;
                if (rangeIssues.Count > 0)
                {
                    report.IsValid = false;
                    report.Issues.AddRange(rangeIssues);
                }

                Logger.LogInfo($"Data validation completed. Valid: {report.IsValid}, Issues: {report.Issues.Count}");
                return report;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error validating data quality", ex);
                return new DataQualityReport
                {
                    IsValid = false,
                    Issues = new List<string> { $"Error during validation: {ex.Message}" }
                };
            }
        }

        private int CheckMissingValues(List<RossmannSalesRecord> data)
        {
            return data.Count(r => r.Sales < 0 || float.IsNaN(r.Sales));
        }

        private int CheckDateContinuity(List<RossmannSalesRecord> data)
        {
            var orderedData = data.OrderBy(r => r.Date).ToList();
            int gaps = 0;

            for (int i = 1; i < orderedData.Count; i++)
            {
                var expectedDate = orderedData[i - 1].Date.AddDays(1);
                if (orderedData[i].Date != expectedDate)
                {
                    gaps++;
                }
            }

            return gaps;
        }

        private List<AnomalyReport> DetectAnomalies(List<RossmannSalesRecord> data)
        {
            var anomalies = new List<AnomalyReport>();
            var sales = data.Select(r => r.Sales).ToList();

            // Calculate statistics
            float mean = sales.Average();
            float stdDev = CalculateStandardDeviation(sales);

            // Detect anomalies using Z-score
            for (int i = 0; i < data.Count; i++)
            {
                float zScore = Math.Abs((sales[i] - mean) / stdDev);
                if (zScore > _zScoreThreshold)
                {
                    anomalies.Add(new AnomalyReport
                    {
                        Date = data[i].Date,
                        Value = sales[i],
                        ZScore = zScore,
                        Description = $"Sales value {sales[i]:F2} is {zScore:F2} standard deviations from mean"
                    });
                }
            }

            return anomalies;
        }

        private List<string> CheckValueRanges(List<RossmannSalesRecord> data)
        {
            var issues = new List<string>();

            // Check for negative sales
            int negativeSales = data.Count(r => r.Sales < 0);
            if (negativeSales > 0)
            {
                issues.Add($"Found {negativeSales} records with negative sales values");
            }

            // Check for unreasonably large values
            float mean = data.Select(r => r.Sales).Average();
            float stdDev = CalculateStandardDeviation(data.Select(r => r.Sales).ToList());
            float upperLimit = mean + (_zScoreThreshold * stdDev);

            int largeValues = data.Count(r => r.Sales > upperLimit);
            if (largeValues > 0)
            {
                issues.Add($"Found {largeValues} records with unusually large sales values (> {upperLimit:F2})");
            }

            return issues;
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
            return (float)Math.Sqrt(sumSquaredDiffs / (values.Count - 1));
        }
    }

    /// <summary>
    /// Contains the results of data quality validation
    /// </summary>
    public class DataQualityReport
    {
        public bool IsValid { get; set; } = true;
        public int TotalRecords { get; set; }
        public int MissingValues { get; set; }
        public int DateGaps { get; set; }
        public List<AnomalyReport> Anomalies { get; set; } = new List<AnomalyReport>();
        public List<string> RangeIssues { get; set; } = new List<string>();
        public List<string> Issues { get; set; } = new List<string>();
        public DateTime ValidationTime { get; set; }

        public override string ToString()
        {
            return $"Data Quality Report ({ValidationTime:yyyy-MM-dd HH:mm:ss}):\n" +
                   $"Valid: {IsValid}\n" +
                   $"Total Records: {TotalRecords}\n" +
                   $"Missing Values: {MissingValues}\n" +
                   $"Date Gaps: {DateGaps}\n" +
                   $"Anomalies: {Anomalies.Count}\n" +
                   $"Range Issues: {RangeIssues.Count}\n" +
                   $"Total Issues: {Issues.Count}";
        }
    }

    /// <summary>
    /// Contains information about a detected anomaly
    /// </summary>
    public class AnomalyReport
    {
        public DateTime Date { get; set; }
        public float Value { get; set; }
        public float ZScore { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd}: {Description} (Z-score: {ZScore:F2})";
        }
    }
} 