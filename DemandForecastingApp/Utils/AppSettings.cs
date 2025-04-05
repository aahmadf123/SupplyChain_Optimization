using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DemandForecastingApp.Utils
{
    /// <summary>
    /// Utility class for managing application settings
    /// </summary>
    public static class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SupplyChainOptimization",
            "settings.xml");
            
        private static readonly Dictionary<string, string> DefaultSettings = new Dictionary<string, string>
        {
            { "DataFolderPath", "" },
            { "ForecastHorizon", "30" },
            { "ForecastModel", "SSA" },
            { "WindowSize", "7" },
            { "SmoothingAlpha", "0.2" },
            { "ShowConfidenceIntervals", "true" },
            { "LeadTime", "3" },
            { "ServiceLevel", "0.95" },
            { "ApiKey", "" }
        };
        
        private static Dictionary<string, string> _settings;
        
        static AppSettings()
        {
            _settings = new Dictionary<string, string>();
            LoadSettings();
        }
        
        /// <summary>
        /// Gets a setting value, or returns the default value if not found
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Default value to return if setting not found</param>
        /// <returns>Setting value or default</returns>
        public static string GetSetting(string key, string defaultValue = null)
        {
            if (_settings.TryGetValue(key, out string value))
            {
                return value;
            }
            
            if (defaultValue != null)
            {
                return defaultValue;
            }
            
            return DefaultSettings.TryGetValue(key, out string defaultVal) ? defaultVal : string.Empty;
        }
        
        /// <summary>
        /// Saves a setting value
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        public static void SaveSetting(string key, string value)
        {
            _settings[key] = value;
            SaveSettings();
        }
        
        /// <summary>
        /// Loads settings from the settings file
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                // Make sure directory exists
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // If settings file doesn't exist, create it with defaults
                if (!File.Exists(SettingsFilePath))
                {
                    CreateDefaultSettings();
                }
                
                // Load settings from file
                XDocument doc = XDocument.Load(SettingsFilePath);
                
                _settings.Clear();
                foreach (var element in doc.Root.Elements("setting"))
                {
                    string key = element.Attribute("key")?.Value;
                    string value = element.Attribute("value")?.Value;
                    
                    if (!string.IsNullOrEmpty(key))
                    {
                        _settings[key] = value ?? string.Empty;
                    }
                }
                
                Logger.LogInfo("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading settings, using defaults", ex);
                
                // Use defaults if there's an error
                _settings = new Dictionary<string, string>(DefaultSettings);
            }
        }
        
        /// <summary>
        /// Saves all settings to the settings file
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                // Make sure directory exists
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                XDocument doc = new XDocument(
                    new XElement("settings")
                );
                
                foreach (var setting in _settings)
                {
                    doc.Root.Add(
                        new XElement("setting",
                            new XAttribute("key", setting.Key),
                            new XAttribute("value", setting.Value ?? string.Empty)
                        )
                    );
                }
                
                // Add any default settings that aren't in _settings
                foreach (var defaultSetting in DefaultSettings)
                {
                    if (!_settings.ContainsKey(defaultSetting.Key))
                    {
                        doc.Root.Add(
                            new XElement("setting",
                                new XAttribute("key", defaultSetting.Key),
                                new XAttribute("value", defaultSetting.Value ?? string.Empty)
                            )
                        );
                    }
                }
                
                doc.Save(SettingsFilePath);
                Logger.LogInfo("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
            }
        }
        
        /// <summary>
        /// Creates a default settings file
        /// </summary>
        private static void CreateDefaultSettings()
        {
            XDocument doc = new XDocument(
                new XElement("settings")
            );
            
            foreach (var setting in DefaultSettings)
            {
                doc.Root.Add(
                    new XElement("setting",
                        new XAttribute("key", setting.Key),
                        new XAttribute("value", setting.Value ?? string.Empty)
                    )
                );
            }
            
            string directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            doc.Save(SettingsFilePath);
        }
        
        /// <summary>
        /// Resets all settings to their default values
        /// </summary>
        public static void ResetToDefaults()
        {
            _settings = new Dictionary<string, string>(DefaultSettings);
            SaveSettings();
        }
    }
}