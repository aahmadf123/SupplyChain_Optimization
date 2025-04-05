using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace DemandForecastingApp.Utils
{
    /// <summary>
    /// Static utility class for logging application events and errors
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SupplyChainOptimization", 
            "Logs");
            
        private static readonly int MaxLogFiles = 5;
        private static readonly int MaxLogSize = 5 * 1024 * 1024; // 5 MB
        
        private static readonly object _lockObj = new object();
        private static string _currentLogFile;
        private static bool _isInitialized = false;
        
        /// <summary>
        /// Initialize the logger and set up log files
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // Create log directory if it doesn't exist
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                
                // Set up the current log file with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(LogDirectory, $"log_{timestamp}.txt");
                
                // Clean up old log files
                CleanupOldLogFiles();
                
                _isInitialized = true;
                
                // Log startup info
                LogInfo($"Logging initialized. Application starting.");
                LogInfo($"OS: {Environment.OSVersion}, CLR: {Environment.Version}");
            }
            catch (Exception ex)
            {
                // If we can't initialize logging, there's not much we can do
                Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Log an informational message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogInfo(string message)
        {
            WriteLogEntry("INFO", message);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">Warning message to log</param>
        public static void LogWarning(string message)
        {
            WriteLogEntry("WARNING", message);
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">Error message to log</param>
        /// <param name="ex">Optional exception to include details from</param>
        public static void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                message = $"{message}\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}";
                
                if (ex.InnerException != null)
                {
                    message += $"\nInner Exception: {ex.InnerException.Message}";
                }
            }
            
            WriteLogEntry("ERROR", message);
        }
        
        /// <summary>
        /// Write a log entry to the log file
        /// </summary>
        /// <param name="level">Log level (INFO, WARNING, ERROR)</param>
        /// <param name="message">Message to log</param>
        private static void WriteLogEntry(string level, string message)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                int threadId = Thread.CurrentThread.ManagedThreadId;
                
                string logEntry = $"[{timestamp}] [{level}] [Thread {threadId}] {message}";
                
                lock (_lockObj)
                {
                    // Check if we need to rotate the log file
                    if (File.Exists(_currentLogFile) && new FileInfo(_currentLogFile).Length > MaxLogSize)
                    {
                        RotateLogFiles();
                    }
                    
                    // Append to log file
                    File.AppendAllText(_currentLogFile, logEntry + Environment.NewLine);
                }
                
                // Also write to debug output
                Debug.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                // If logging fails, just write to debug output
                Debug.WriteLine($"Logging failed: {ex.Message}");
                Debug.WriteLine($"Original message: {message}");
            }
        }
        
        /// <summary>
        /// Rotate log files when current log gets too large
        /// </summary>
        private static void RotateLogFiles()
        {
            try
            {
                // Create a new log file with current timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(LogDirectory, $"log_{timestamp}.txt");
                
                // Clean up old log files
                CleanupOldLogFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log rotation failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Remove old log files to keep disk usage under control
        /// </summary>
        private static void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(LogDirectory, "log_*.txt")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToArray();
                
                // Keep only MaxLogFiles most recent logs
                for (int i = MaxLogFiles; i < logFiles.Length; i++)
                {
                    logFiles[i].Delete();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log cleanup failed: {ex.Message}");
            }
        }
    }
}