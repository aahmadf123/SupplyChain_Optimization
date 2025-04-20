using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DemandForecastingApp.Utils
{
    public class InventoryStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float currentInventory && parameter is float reorderPoint)
            {
                // Critical: < 50% of reorder point
                if (currentInventory < reorderPoint * 0.5f)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                // Warning: Between 50% and 100% of reorder point
                else if (currentInventory < reorderPoint)
                {
                    return new SolidColorBrush(Colors.Orange);
                }
                // Good: Between 100% and 200% of reorder point
                else if (currentInventory <= reorderPoint * 2.0f)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                // Excess: > 200% of reorder point
                else
                {
                    return new SolidColorBrush(Colors.Blue);
                }
            }
            
            // String parameter conversion for text representations
            if (value is float currentLevel && parameter is string mode)
            {
                float reorderLevel = 20; // Default value if not provided
                
                // Try to extract reorder level from parameter
                if (mode.Contains(":"))
                {
                    string[] parts = mode.Split(':');
                    if (parts.Length > 1 && float.TryParse(parts[1], out float level))
                    {
                        reorderLevel = level;
                    }
                    mode = parts[0].Trim();
                }
                
                if (mode.Equals("text", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentLevel < reorderLevel * 0.5f)
                    {
                        return "CRITICAL";
                    }
                    else if (currentLevel < reorderLevel)
                    {
                        return "WARNING";
                    }
                    else if (currentLevel <= reorderLevel * 2.0f)
                    {
                        return "GOOD";
                    }
                    else
                    {
                        return "EXCESS";
                    }
                }
            }
            
            // Default return value
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack not implemented for InventoryStatusConverter");
        }
    }
}