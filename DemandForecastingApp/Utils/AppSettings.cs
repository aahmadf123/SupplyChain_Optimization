using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DemandForecastingApp.Utils
{
    public static class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SupplyChainOptimization",
            "settings.json");
            
        private static Dictionary<string, string>? _settings;
        
        static AppSettings()
        {
            LoadSettings();
        }
        
        public static string GetSetting(string key, string? defaultValue = null)
        {
            if (_settings != null && _settings.TryGetValue(key, out string? value))
            {
                return value;
            }
            return defaultValue ?? string.Empty;
        }
        
        public static void SaveSetting(string key, string value)
        {
            if (_settings == null)
            {
                _settings = new Dictionary<string, string>();
            }
            
            _settings[key] = value;
            SaveSettings();
        }
        
        private static void LoadSettings()
        {
            try
            {
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                }
                
                // If settings is still null, create a new dictionary with defaults
                if (_settings == null)
                {
                    _settings = new Dictionary<string, string>();
                    
                    // Add default settings including API key
                    _settings["AlphaVantageApiKey"] = "APFCJIALTDC7YYUT";
                    
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error loading settings", ex);
                _settings = new Dictionary<string, string>();
            }
        }
        
        private static void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error saving settings", ex);
            }
        }
    }
}