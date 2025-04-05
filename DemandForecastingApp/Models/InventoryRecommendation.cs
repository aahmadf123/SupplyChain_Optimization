using System;

namespace DemandForecastingApp.Models
{
    /// <summary>
    /// Model class representing inventory recommendations based on forecasting results
    /// </summary>
    public class InventoryRecommendation
    {
        /// <summary>
        /// Identifier for the item
        /// </summary>
        public string Item { get; set; }
        
        /// <summary>
        /// Current inventory level
        /// </summary>
        public int CurrentStock { get; set; }
        
        /// <summary>
        /// Inventory level at which replenishment order should be placed
        /// </summary>
        public float ReorderPoint { get; set; }
        
        /// <summary>
        /// Expected demand during lead time
        /// </summary>
        public float LeadTimeDemand { get; set; }
        
        /// <summary>
        /// Safety stock level to maintain
        /// </summary>
        public float SafetyStock { get; set; }
        
        /// <summary>
        /// Recommended order quantity to place
        /// </summary>
        public int RecommendedOrder { get; set; }
        
        /// <summary>
        /// Gets the stock status based on current inventory levels
        /// </summary>
        public string StockStatus
        {
            get
            {
                if (CurrentStock <= SafetyStock)
                {
                    return "Critical";
                }
                else if (CurrentStock <= ReorderPoint)
                {
                    return "Low";
                }
                else
                {
                    return "Adequate";
                }
            }
        }
        
        /// <summary>
        /// Days of supply at current inventory levels
        /// </summary>
        public int DaysOfSupply
        {
            get
            {
                // Approximate days of supply based on lead time demand
                if (LeadTimeDemand <= 0)
                {
                    return 0;
                }
                
                float dailyDemand = LeadTimeDemand / 3; // Assuming lead time of 3 days by default
                return (int)Math.Ceiling(CurrentStock / dailyDemand);
            }
        }
    }
}