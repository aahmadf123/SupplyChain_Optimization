using System;
using System.IO;
using DemandForecastingApp.Utils;

namespace DemandForecastingApp.Services
{
    public static class DataService
    {
        private static readonly string DEFAULT_DATA_FOLDER = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SupplyChainOptimization",
            "Data");
            
        /// <summary>
        /// Gets the path to the data folder, creating it if it doesn't exist
        /// </summary>
        /// <returns>Path to the data folder</returns>
        public static string GetDataFolderPath()
        {
            try
            {
                // First check if user has specified a custom data path in settings
                string customPath = AppSettings.GetSetting("DataFolderPath");
                
                string dataFolderPath = string.IsNullOrEmpty(customPath)
                    ? DEFAULT_DATA_FOLDER
                    : customPath;
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(dataFolderPath))
                {
                    Logger.LogInfo($"Creating data directory: {dataFolderPath}");
                    Directory.CreateDirectory(dataFolderPath);
                }
                
                return dataFolderPath;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting data folder path", ex);
                throw;
            }
        }
        
        /// <summary>
        /// Verifies that all required Rossmann data files exist in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to check for data files</param>
        /// <returns>True if all required files exist</returns>
        public static bool VerifyRossmannDataFiles(string folderPath)
        {
            try
            {
                string[] requiredFiles = { "train.csv", "store.csv" };
                
                foreach (var fileName in requiredFiles)
                {
                    string filePath = Path.Combine(folderPath, fileName);
                    
                    if (!File.Exists(filePath))
                    {
                        Logger.LogWarning($"Required file not found: {filePath}");
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error verifying data files", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Creates sample data files for demonstration purposes
        /// </summary>
        /// <param name="folderPath">Path where to create the sample files</param>
        /// <returns>True if files were created successfully</returns>
        public static bool CreateSampleDataFiles(string folderPath)
        {
            try
            {
                // Create the directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                // Create sample train.csv
                string trainFilePath = Path.Combine(folderPath, "train.csv");
                using (StreamWriter writer = new StreamWriter(trainFilePath))
                {
                    // Write header
                    writer.WriteLine("Store,DayOfWeek,Date,Sales,Customers,Open,Promo,StateHoliday,SchoolHoliday");
                    
                    // Generate sample data (30 days for 3 stores)
                    var random = new Random(42);
                    DateTime startDate = DateTime.Today.AddDays(-30);
                    
                    for (int day = 0; day < 30; day++)
                    {
                        DateTime date = startDate.AddDays(day);
                        int dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;
                        
                        for (int store = 1; store <= 3; store++)
                        {
                            // Sales vary by day of week and have some randomness
                            float sales = dayOfWeek switch
                            {
                                1 => 5000 + random.Next(-500, 500), // Monday
                                2 => 5200 + random.Next(-500, 500), // Tuesday
                                3 => 5500 + random.Next(-500, 500), // Wednesday
                                4 => 5800 + random.Next(-500, 500), // Thursday
                                5 => 7000 + random.Next(-800, 800), // Friday
                                6 => 8500 + random.Next(-1000, 1000), // Saturday
                                7 => 4000 + random.Next(-400, 400), // Sunday
                                _ => 5000
                            };
                            
                            int customers = (int)(sales / 40 + random.Next(-10, 10));
                            int open = 1;
                            int promo = random.Next(0, 2);
                            string stateHoliday = "0";
                            int schoolHoliday = 0;
                            
                            // Simulate holidays
                            if (date.Month == 1 && date.Day == 1)
                            {
                                stateHoliday = "a";  // New Year's
                                open = 0;
                            }
                            else if (date.Month == 12 && date.Day == 25)
                            {
                                stateHoliday = "b";  // Christmas
                                open = 0;
                            }
                            
                            // Write the record
                            writer.WriteLine($"{store},{dayOfWeek},{date:yyyy-MM-dd},{sales},{customers},{open},{promo},{stateHoliday},{schoolHoliday}");
                        }
                    }
                }
                
                // Create sample store.csv
                string storeFilePath = Path.Combine(folderPath, "store.csv");
                using (StreamWriter writer = new StreamWriter(storeFilePath))
                {
                    // Write header
                    writer.WriteLine("Store,StoreType,Assortment,CompetitionDistance,CompetitionOpenSinceMonth,CompetitionOpenSinceYear,Promo2,Promo2SinceWeek,Promo2SinceYear,PromoInterval");
                    
                    // Generate sample data (3 stores)
                    string[] storeTypes = { "a", "b", "c" };
                    string[] assortments = { "a", "b", "c" };
                    
                    for (int store = 1; store <= 3; store++)
                    {
                        string storeType = storeTypes[store % storeTypes.Length];
                        string assortment = assortments[store % assortments.Length];
                        int competitionDistance = store * 1000 + 500;
                        int competitionOpenSinceMonth = (store % 12) + 1;
                        int competitionOpenSinceYear = 2010 + (store % 10);
                        int promo2 = store % 2;
                        int promo2SinceWeek = (store % 52) + 1;
                        int promo2SinceYear = 2010 + (store % 10);
                        string promoInterval = store % 2 == 0 ? "Jan,Apr,Jul,Oct" : "Feb,May,Aug,Nov";
                        
                        // Write the record
                        writer.WriteLine($"{store},{storeType},{assortment},{competitionDistance},{competitionOpenSinceMonth},{competitionOpenSinceYear},{promo2},{promo2SinceWeek},{promo2SinceYear},{promoInterval}");
                    }
                }
                
                Logger.LogInfo($"Sample data files created in {folderPath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error creating sample data files", ex);
                return false;
            }
        }
    }
}