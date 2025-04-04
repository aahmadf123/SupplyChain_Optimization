using System;
using System.IO;
using System.Text;

namespace DemandForecastingApp.Utils
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SupplyChainOptimization",
            "app.log");
            
        private static readonly object FileLock = new object();
        
        static Logger()
        {
            // Create the directory if it doesn't exist
            string directory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Initialize log file with a header
            if (!File.Exists(LogFilePath))
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, false))
                    {
                        writer.WriteLine("===== Application Log Started at {0} =====", DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to initialize log file: " + ex.Message);
                }
            }
        }
        
        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }
        
        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }
        
        public static void LogError(string message, Exception ex = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                sb.AppendLine();
                sb.Append("StackTrace: ").Append(ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.Append("InnerException: ").Append(ex.InnerException.GetType().Name)
                      .Append(": ").Append(ex.InnerException.Message);
                }
            }
            
            Log("ERROR", sb.ToString());
        }
        
        private static void Log(string level, string message)
        {
            try
            {
                lock (FileLock)
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine("[{0}] {1}: {2}", 
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            level,
                            message);
                    }
                }
                
                // Also write to console for debugging
                Console.WriteLine("[{0}] {1}: {2}", 
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    level,
                    message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to write to log file: " + ex.Message);
            }
        }
    }
}