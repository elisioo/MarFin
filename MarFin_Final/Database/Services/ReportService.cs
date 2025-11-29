using Microsoft.Data.SqlClient;
using System.Data;

namespace MarFin_Final.Database.Services;

public class ReportService
{
    private readonly string _connectionString = "Server=LAPTOP-FQVN9QLT\\SQLEXPRESS;Database=MarFin_DB;Trusted_Connection=true;Encrypt=false;";

    // Revenue Report Data
    public async Task<List<RevenueData>> GetRevenueByMonthAsync(DateTime startDate, DateTime endDate)
    {
        var revenueData = new List<RevenueData>();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        MONTH(created_date) as Month,
                        YEAR(created_date) as Year,
                        ISNULL(SUM(CASE WHEN transaction_type = 'Income' THEN amount ELSE 0 END), 0) as Income,
                        ISNULL(SUM(CASE WHEN transaction_type = 'Expense' THEN amount ELSE 0 END), 0) as Expense
                    FROM tbl_Invoices
                    WHERE created_date BETWEEN @StartDate AND @EndDate
                    GROUP BY MONTH(created_date), YEAR(created_date)
                    ORDER BY Year, Month";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            revenueData.Add(new RevenueData
                            {
                                Month = reader.GetInt32(0),
                                Year = reader.GetInt32(1),
                                Income = reader.GetDecimal(2),
                                Expense = reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching revenue data: {ex.Message}");
        }

        return revenueData;
    }

    // Campaign Performance Data
    public async Task<CampaignPerformanceData> GetCampaignPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        var data = new CampaignPerformanceData();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ISNULL(AVG(CAST(total_opened AS FLOAT) / NULLIF(total_sent, 0) * 100), 0) as OpenRate,
                        ISNULL(AVG(CAST(total_clicked AS FLOAT) / NULLIF(total_sent, 0) * 100), 0) as ClickRate,
                        ISNULL(AVG(CAST(total_converted AS FLOAT) / NULLIF(total_sent, 0) * 100), 0) as ConversionRate,
                        ISNULL(AVG(CAST(revenue_generated AS FLOAT) / NULLIF(actual_spend, 0) * 100), 0) as ROI
                    FROM tbl_Campaigns
                    WHERE created_date BETWEEN @StartDate AND @EndDate
                    AND is_archived = 0";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            data.OpenRate = Math.Round(reader.GetDouble(0), 2);
                            data.ClickRate = Math.Round(reader.GetDouble(1), 2);
                            data.ConversionRate = Math.Round(reader.GetDouble(2), 2);
                            data.ROI = Math.Round(reader.GetDouble(3), 2);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching campaign performance: {ex.Message}");
        }

        return data;
    }

    // Top Customers Data
    public async Task<List<TopCustomerData>> GetTopCustomersAsync(DateTime startDate, DateTime endDate, int limit = 5)
    {
        var customers = new List<TopCustomerData>();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT TOP (@Limit)
                        ROW_NUMBER() OVER (ORDER BY SUM(amount) DESC) as Rank,
                        customer_name,
                        SUM(amount) as TotalAmount,
                        COUNT(*) as TransactionCount
                    FROM tbl_Invoices
                    WHERE created_date BETWEEN @StartDate AND @EndDate
                    GROUP BY customer_name
                    ORDER BY TotalAmount DESC";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    cmd.Parameters.AddWithValue("@Limit", limit);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new TopCustomerData
                            {
                                Rank = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Amount = reader.GetDecimal(2),
                                Transactions = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching top customers: {ex.Message}");
        }

        return customers;
    }

    // Sales Opportunities Data
    public async Task<SalesOpportunitiesData> GetSalesOpportunitiesAsync(DateTime startDate, DateTime endDate)
    {
        var data = new SalesOpportunitiesData();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        COUNT(CASE WHEN opportunity_status = 'Active' THEN 1 END) as Active,
                        COUNT(CASE WHEN opportunity_status = 'Won' THEN 1 END) as Won,
                        COUNT(CASE WHEN opportunity_status = 'Lost' THEN 1 END) as Lost,
                        ISNULL(SUM(opportunity_value), 0) as TotalValue
                    FROM tbl_SalesPipeline
                    WHERE created_date BETWEEN @StartDate AND @EndDate";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            data.Active = reader.GetInt32(0);
                            data.Won = reader.GetInt32(1);
                            data.Lost = reader.GetInt32(2);
                            data.TotalPipelineValue = reader.GetDecimal(3);
                            data.WinRate = data.Active + data.Won > 0 
                                ? Math.Round((double)data.Won / (data.Active + data.Won) * 100, 2) 
                                : 0;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching sales opportunities: {ex.Message}");
        }

        return data;
    }

    // Customer Segments Data
    public async Task<List<CustomerSegmentData>> GetCustomerSegmentsAsync(DateTime startDate, DateTime endDate)
    {
        var segments = new List<CustomerSegmentData>();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        segment_name,
                        COUNT(*) as Count,
                        CAST(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM tbl_Customers WHERE created_date BETWEEN @StartDate AND @EndDate) AS DECIMAL(5,2)) as Percentage
                    FROM tbl_Customers
                    WHERE created_date BETWEEN @StartDate AND @EndDate
                    GROUP BY segment_name
                    ORDER BY Count DESC";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            segments.Add(new CustomerSegmentData
                            {
                                Name = reader.GetString(0),
                                Count = reader.GetInt32(1),
                                Percentage = reader.GetDecimal(2)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching customer segments: {ex.Message}");
        }

        return segments;
    }

    // Revenue by Source Data
    public async Task<List<RevenueSourceData>> GetRevenueBySourceAsync(DateTime startDate, DateTime endDate)
    {
        var sources = new List<RevenueSourceData>();

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        invoice_source,
                        CAST(SUM(amount) * 100.0 / (SELECT SUM(amount) FROM tbl_Invoices WHERE created_date BETWEEN @StartDate AND @EndDate) AS DECIMAL(5,2)) as Percentage,
                        SUM(amount) as TotalAmount
                    FROM tbl_Invoices
                    WHERE created_date BETWEEN @StartDate AND @EndDate
                    GROUP BY invoice_source
                    ORDER BY TotalAmount DESC";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sources.Add(new RevenueSourceData
                            {
                                Name = reader.GetString(0),
                                Percentage = reader.GetDecimal(1),
                                Amount = reader.GetDecimal(2)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching revenue sources: {ex.Message}");
        }

        return sources;
    }

    // Data Models
    public class RevenueData
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class CampaignPerformanceData
    {
        public double OpenRate { get; set; }
        public double ClickRate { get; set; }
        public double ConversionRate { get; set; }
        public double ROI { get; set; }
    }

    public class TopCustomerData
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public int Transactions { get; set; }
    }

    public class SalesOpportunitiesData
    {
        public int Active { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public double WinRate { get; set; }
    }

    public class CustomerSegmentData
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RevenueSourceData
    {
        public string Name { get; set; } = "";
        public decimal Percentage { get; set; }
        public decimal Amount { get; set; }
    }
}
