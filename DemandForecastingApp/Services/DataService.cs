using System;
using System.IO;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Services
{
    public class DataService
    {
        // Get path to the Data folder
        public static string GetDataFolderPath()
        {
            try
            {
                // First try: Check in the application directory
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolderPath = Path.Combine(baseDirectory, "Data");
                
                if (Directory.Exists(dataFolderPath))
                {
                    return dataFolderPath;
                }
                
                // Second try: Check in the project directory (for debug mode)
                string projectDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\.."));
                dataFolderPath = Path.Combine(projectDirectory, "Data");
                
                if (Directory.Exists(dataFolderPath))
                {
                    return dataFolderPath;
                }
                
                // Third try: Check absolute path
                string? directoryName = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                string absolutePath = directoryName != null
                    ? Path.Combine(directoryName, "Data")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                    
                if (Directory.Exists(absolutePath))
                {
                    return absolutePath;
                }
                
                // Fourth try: Check one level up from base directory
                DirectoryInfo? parentDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
                string oneUp = parentDir != null
                    ? Path.Combine(parentDir.FullName, "Data")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                    
                if (Directory.Exists(oneUp))
                {
                    return oneUp;
                }
                
                // If we get here, we couldn't find the Data folder
                throw new DirectoryNotFoundException("Could not locate Data folder");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error locating Data folder", ex);
                throw;
            }
        }
        
        // Check if all required Rossmann data files exist
        public static bool VerifyRossmannDataFiles(string dataFolderPath)
        {
            try
            {
                string trainFile = Path.Combine(dataFolderPath, "train.csv");
                string testFile = Path.Combine(dataFolderPath, "test.csv");
                string storeFile = Path.Combine(dataFolderPath, "store.csv");
                
                return File.Exists(trainFile) && File.Exists(testFile) && File.Exists(storeFile);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error verifying Rossmann data files", ex);
                return false;
            }
        }
    }
}