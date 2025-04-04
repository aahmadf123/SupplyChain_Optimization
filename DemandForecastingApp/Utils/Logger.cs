using System;
using System.IO;
using System.Text;

namespace DemandForecastingApp.Utils
{
    public static class Logger
    {
        private static readonly string LogFilePath = "application.log";
        
        static Logger()
        {
            // Create log directory if it doesn't exist
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }
        
        public static void LogError(string message, Exception ex = null)
        {
            string errorMessage = ex != null ? $"{message} - Exception: {ex.Message}" : message;
            Log("ERROR", errorMessage);
            
            if (ex != null)
            {
                Log("ERROR", $"Stack Trace: {ex.StackTrace}");
            }
        }
        
        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }
        
        private static void Log(string level, string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                
                // Append to log file
                using (StreamWriter writer = new StreamWriter(LogFilePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch
            {
                // Silently fail if logging itself fails
            }
        }
    }
}