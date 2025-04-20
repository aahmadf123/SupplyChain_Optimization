using System.Collections.Generic;
using System.Threading.Tasks;
using DemandForecastingApp.Models;

namespace DemandForecastingApp.Services
{
    public interface IDataService
    {
        /// <summary>
        /// Loads demand data from the configured data source
        /// </summary>
        /// <returns>A list of demand records</returns>
        Task<List<DemandRecord>> LoadDemandDataAsync();

        /// <summary>
        /// Exports forecast results to the configured output location
        /// </summary>
        /// <param name="forecastData">The forecast data to export</param>
        Task ExportForecastResultsAsync(IEnumerable<DemandRecord> forecastData);

        /// <summary>
        /// Gets the path to the data folder
        /// </summary>
        /// <returns>The full path to the data folder</returns>
        string GetDataFolderPath();

        /// <summary>
        /// Verifies that all required data files exist
        /// </summary>
        /// <returns>True if all required files exist, false otherwise</returns>
        bool VerifyRequiredDataFiles();

        /// <summary>
        /// Creates sample data files for demonstration purposes
        /// </summary>
        /// <returns>True if sample data was created successfully, false otherwise</returns>
        bool CreateSampleDataFiles();
    }
} 