namespace MarFin_Final.Database.Models
{
    /// <summary>
    /// Represents a financial data point for OHLC (Open, High, Low, Close) candlestick charts
    /// </summary>
    public class FinancialPoint
    {
        /// <summary>
        /// Date of the financial data point
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// Highest price during the period
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Lowest price during the period
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Trading volume (optional)
        /// </summary>
        public long? Volume { get; set; }
    }
}
