# Supply Chain Optimization: Demand Forecasting Application

## Overview
This application empowers supply chain professionals to optimize inventory management and demand forecasting using advanced data-driven analytics. Leveraging historical sales data from Rossmann stores and market sector performance provides actionable insights to enhance supply chain efficiency and decision-making.

## Purpose
Supply chain managers consistently face challenges in balancing inventory levels:
- Excess inventory ties up valuable capital.
- Insufficient inventory results in stockouts and lost sales.

This application addresses these challenges by:
- Analyzing historical sales data to forecast future demand.
- Providing inventory recommendations based on lead times and reorder thresholds.
- Integrating market performance indicators to understand external impacts on demand.
- Supporting informed, data-driven decision-making to optimize supply chain performance.

## Features
- **Data Import & Processing:** Seamlessly import and preprocess Rossmann store sales data.
- **Demand Forecasting:** Accurately forecast future product demand using machine learning.
- **Market Analysis:** Incorporate external market data (Alpha Vantage API) to identify trends affecting demand.
- **Inventory Recommendations:** Calculate optimal inventory levels based on forecasted demand and configurable parameters.
- **Interactive Visualization:** Visualize trends, forecasts, and inventory levels using intuitive charts and tables.
- **Customizable Settings:** Adjust lead times, reorder thresholds, forecast horizons, and machine learning parameters easily.

## Technologies
- **Frontend:** WPF (Windows Presentation Foundation), XAML
- **Backend:** C#, .NET Framework
- **Machine Learning:** ML.NET (Regression, Forecasting Algorithms), optional LSTM via external integration
- **Data Handling:** CSV file processing, structured data models
- **External API:** Alpha Vantage (Market Data Integration)

## Mathematical Equations
### Economic Order Quantity (EOQ)
Determines optimal order quantity minimizing total inventory costs:

$$
EOQ = \sqrt{\frac{2DS}{H}}
$$
- \( D \): Annual demand
- \( S \): Ordering cost per order
- \( H \): Holding cost per unit per year

### Reorder Point (ROP)
Indicates inventory level to trigger a new order:

$$
ROP = (d \times L) + SS
$$
- \( d \): Average daily demand
- \( L \): Lead time (in days)
- \( SS \): Safety stock

### Safety Stock (SS)
Buffer stock to handle variability in demand and supply:

$$
SS = Z \times \sigma_{dL}
$$
- \( Z \): Desired service level (number of standard deviations, e.g., Z = 1.65 for 95%)
- \( \sigma_{dL} \): Standard deviation of demand during lead time

### Forecasting Accuracy Metrics
#### Root Mean Squared Error (RMSE)

$$
RMSE = \sqrt{\frac{1}{n}\sum_{i=1}^{n}(y_i - \hat{y_i})^2}
$$

#### Mean Absolute Error (MAE)

$$
MAE = \frac{1}{n}\sum_{i=1}^{n}|y_i - \hat{y_i}|
$$
- \( y_i \): Actual value
- \( \hat{y_i} \): Forecasted value
- \( n \): Number of observations

## Installation
### Prerequisites:
- Windows OS
- .NET Framework 4.7.2 or later
- Visual Studio 2019 or later (recommended for development)

### Steps:
1. Download or clone the repository.
2. Extract contents to your preferred location.
3. Ensure the data files (`train.csv`, `test.csv`, `store.csv`) are placed in the `Data` folder.
4. Open the solution (`.sln`) file with Visual Studio, or execute `DemandForecastingApp.exe` directly.

## How to Use
### Step 1: Configure Application
- Go to the **Settings** tab.
- Enter your Alpha Vantage API key (obtain your own for higher reliability).
- Set your desired forecast horizon (default: 12 months).
- Adjust ML model parameters as required.
- Click **Save Settings**.

### Step 2: Load Data
- Click **Load Data**.
- The application will import:
  - `train.csv`: Historical sales data.
  - `test.csv`: Validation dataset.
  - `store.csv`: Store metadata.
- Verify successful data import via the confirmation message.

### Step 3: Run Forecast
- Enter your desired **Lead Time** and **Reorder Threshold**.
- Click **Run Forecast**.
- Wait for processing; the application generates demand forecasts and inventory recommendations.

### Step 4: Analyze Results
- Navigate between tabs:
  - **Forecast Details:** View detailed forecasting results.
  - **Inventory Recommendations:** Evaluate suggested inventory levels.
  - **Market Analysis:** Review sector performance and its impact on demand.
- Use visualizations to identify patterns and insights.
- Refresh market data using the **Refresh Data** button as necessary.

## Data Files
Place the following files in the `Data` folder:
- `train.csv`: Historical sales data (columns: Store, DayOfWeek, Date, Sales, Customers, Open, etc.).
- `test.csv`: Validation dataset (structure similar to `train.csv`).
- `store.csv`: Store metadata (StoreType, Competition details, Promo information, etc.).

## Troubleshooting
- **Data Loading Issues:** Verify CSV formatting and file placement in the `Data` folder.
- **API Key Issues:** Confirm your Alpha Vantage API key in the Settings tab.
- **Forecasting Errors:** Ensure correct data import before executing forecasts.
- **UI Issues:** Adjust theme or display settings for improved visibility.

## Developer Notes
- **Main Logic:** Located in `MainViewModel.cs` (MVVM pattern).
- **Data Importing:** Managed by the `RossmannDataImporter` class (`/Data`).
- **Forecast Algorithms:** Contained in the `ForecastEngine` namespace (`/Models`).
- **UI Architecture:** Adheres to MVVM for clear separation of concerns and maintainability.

## Future Enhancements
- Additional forecasting models (e.g., advanced deep learning LSTM integration)
- Reporting and data export capabilities
- ERP integration for broader system connectivity
- Mobile app companion for real-time monitoring

