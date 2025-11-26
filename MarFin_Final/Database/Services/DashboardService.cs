using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MarFin_Final.Database.Services
{
    public class DashboardService
    {
        private readonly string _connectionString;

        public DashboardService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Admin Dashboard Stats
        public async Task<AdminDashboardStats> GetAdminStatsAsync(DateTime startDate, DateTime endDate)
        {
            var stats = new AdminDashboardStats();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get total customers
                var customerCmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) as TotalCustomers,
                        COUNT(CASE WHEN created_date >= @StartDate THEN 1 END) as NewCustomers
                    FROM tbl_Customers 
                    WHERE is_archived = 0", connection);
                customerCmd.Parameters.AddWithValue("@StartDate", startDate);

                using (var reader = await customerCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.TotalCustomers = reader.GetInt32(0);
                        stats.NewCustomers = reader.GetInt32(1);
                    }
                }

                // Get revenue stats
                var revenueCmd = new SqlCommand(@"
                    SELECT 
                        ISNULL(SUM(total_amount), 0) as TotalRevenue,
                        ISNULL(SUM(CASE WHEN invoice_date >= @StartDate THEN total_amount ELSE 0 END), 0) as PeriodRevenue
                    FROM tbl_Invoices 
                    WHERE is_archived = 0 AND payment_status = 'Paid'", connection);
                revenueCmd.Parameters.AddWithValue("@StartDate", startDate);

                using (var reader = await revenueCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.TotalRevenue = reader.GetDecimal(0);
                        stats.PeriodRevenue = reader.GetDecimal(1);
                    }
                }

                // Get campaign stats
                var campaignCmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) as TotalCampaigns,
                        COUNT(CASE WHEN campaign_status = 'Active' THEN 1 END) as ActiveCampaigns
                    FROM tbl_Campaigns 
                    WHERE is_archived = 0", connection);

                using (var reader = await campaignCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.TotalCampaigns = reader.GetInt32(0);
                        stats.ActiveCampaigns = reader.GetInt32(1);
                    }
                }

                // Get conversion rate
                var conversionCmd = new SqlCommand(@"
                    SELECT 
                        COUNT(DISTINCT customer_id) as TotalCustomers,
                        COUNT(DISTINCT CASE WHEN total_revenue > 0 THEN customer_id END) as ConvertedCustomers
                    FROM tbl_Customers 
                    WHERE is_archived = 0", connection);

                using (var reader = await conversionCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var total = reader.GetInt32(0);
                        var converted = reader.GetInt32(1);
                        stats.ConversionRate = total > 0 ? (converted * 100.0m / total) : 0;
                    }
                }
            }

            return stats;
        }

        // Finance Dashboard Stats
        public async Task<FinanceDashboardStats> GetFinanceStatsAsync(DateTime startDate, DateTime endDate)
        {
            var stats = new FinanceDashboardStats();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT 
                        ISNULL(SUM(CASE WHEN payment_status = 'Paid' THEN total_amount ELSE 0 END), 0) as TotalRevenue,
                        COUNT(CASE WHEN invoice_date BETWEEN @StartDate AND @EndDate THEN 1 END) as TotalInvoices,
                        ISNULL(SUM(CASE WHEN payment_status IN ('Pending', 'Issued') THEN total_amount ELSE 0 END), 0) as PendingAmount,
                        ISNULL(SUM(CASE WHEN payment_status = 'Overdue' THEN total_amount ELSE 0 END), 0) as OverdueAmount
                    FROM tbl_Invoices 
                    WHERE is_archived = 0", connection);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.TotalRevenue = reader.GetDecimal(0);
                        stats.TotalInvoices = reader.GetInt32(1);
                        stats.PendingAmount = reader.GetDecimal(2);
                        stats.OverdueAmount = reader.GetDecimal(3);
                    }
                }

                // Get transaction count
                var transCmd = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM tbl_Transactions 
                    WHERE is_archived = 0 
                    AND transaction_date BETWEEN @StartDate AND @EndDate", connection);
                transCmd.Parameters.AddWithValue("@StartDate", startDate);
                transCmd.Parameters.AddWithValue("@EndDate", endDate);

                stats.TotalTransactions = (int)await transCmd.ExecuteScalarAsync();
            }

            return stats;
        }

        // Marketing Dashboard Stats
        public async Task<MarketingDashboardStats> GetMarketingStatsAsync(DateTime startDate, DateTime endDate)
        {
            var stats = new MarketingDashboardStats();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get customer stats
                var customerCmd = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM tbl_Customers 
                    WHERE is_archived = 0", connection);
                stats.TotalCustomers = (int)await customerCmd.ExecuteScalarAsync();

                // Get campaign stats
                var campaignCmd = new SqlCommand(@"
                    SELECT 
                        COUNT(CASE WHEN campaign_status = 'Active' THEN 1 END) as ActiveCampaigns,
                        ISNULL(SUM(total_sent), 0) as TotalEmailsSent,
                        ISNULL(SUM(total_opened), 0) as TotalOpened,
                        ISNULL(SUM(total_clicked), 0) as TotalClicked,
                        ISNULL(SUM(total_converted), 0) as TotalConverted
                    FROM tbl_Campaigns 
                    WHERE is_archived = 0 
                    AND start_date >= @StartDate", connection);
                campaignCmd.Parameters.AddWithValue("@StartDate", startDate);

                using (var reader = await campaignCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.ActiveCampaigns = reader.GetInt32(0);
                        stats.TotalEmailsSent = reader.GetInt32(1);
                        var totalOpened = reader.GetInt32(2);
                        var totalSent = stats.TotalEmailsSent;
                        stats.ConversionRate = totalSent > 0 ? (totalOpened * 100.0m / totalSent) : 0;
                    }
                }
            }

            return stats;
        }

        // Sales Dashboard Stats
        public async Task<SalesDashboardStats> GetSalesStatsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var stats = new SalesDashboardStats();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) as TotalOpportunities,
                        ISNULL(SUM(deal_value), 0) as PipelineValue,
                        COUNT(CASE WHEN actual_close_date IS NOT NULL AND actual_close_date BETWEEN @StartDate AND @EndDate THEN 1 END) as ClosedDeals,
                        COUNT(CASE WHEN actual_close_date IS NOT NULL THEN 1 END) as TotalClosedDeals
                    FROM tbl_Sales_Pipeline 
                    WHERE assigned_to = @UserId 
                    AND is_archived = 0", connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.TotalOpportunities = reader.GetInt32(0);
                        stats.PipelineValue = reader.GetDecimal(1);
                        stats.ClosedDeals = reader.GetInt32(2);
                        var totalClosed = reader.GetInt32(3);
                        var totalOpps = stats.TotalOpportunities;
                        stats.WinRate = totalOpps > 0 ? (totalClosed * 100.0m / totalOpps) : 0;
                    }
                }
            }

            return stats;
        }

        // Get Revenue Trend Data
        public async Task<List<ChartData>> GetRevenueTrendAsync(DateTime startDate, DateTime endDate, string period = "monthly")
        {
            var data = new List<ChartData>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = period switch
                {
                    "yearly" => @"
                        SELECT 
                            CAST(YEAR(invoice_date) AS VARCHAR) as Label,
                            ISNULL(SUM(total_amount), 0) as Value
                        FROM tbl_Invoices 
                        WHERE is_archived = 0 
                        AND payment_status = 'Paid'
                        AND invoice_date BETWEEN @StartDate AND @EndDate
                        GROUP BY YEAR(invoice_date)
                        ORDER BY YEAR(invoice_date)",

                    "quarterly" => @"
                        SELECT 
                            CAST(YEAR(invoice_date) AS VARCHAR) + ' Q' + CAST(DATEPART(QUARTER, invoice_date) AS VARCHAR) as Label,
                            ISNULL(SUM(total_amount), 0) as Value
                        FROM tbl_Invoices 
                        WHERE is_archived = 0 
                        AND payment_status = 'Paid'
                        AND invoice_date BETWEEN @StartDate AND @EndDate
                        GROUP BY YEAR(invoice_date), DATEPART(QUARTER, invoice_date)
                        ORDER BY YEAR(invoice_date), DATEPART(QUARTER, invoice_date)",

                    _ => @"
                        SELECT 
                            FORMAT(invoice_date, 'MMM yyyy') as Label,
                            ISNULL(SUM(total_amount), 0) as Value
                        FROM tbl_Invoices 
                        WHERE is_archived = 0 
                        AND payment_status = 'Paid'
                        AND invoice_date BETWEEN @StartDate AND @EndDate
                        GROUP BY YEAR(invoice_date), MONTH(invoice_date), FORMAT(invoice_date, 'MMM yyyy')
                        ORDER BY YEAR(invoice_date), MONTH(invoice_date)"
                };

                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData
                        {
                            Label = reader.GetString(0),
                            Value = reader.GetDecimal(1)
                        });
                    }
                }
            }

            return data;
        }

        // Get Recent Activities
        public async Task<List<ActivityItem>> GetRecentActivitiesAsync(string role, int? userId = null, int limit = 5)
        {
            var activities = new List<ActivityItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = role switch
                {
                    "Finance" => @"
                        SELECT TOP (@Limit)
                            'Payment received from ' + c.company_name as Text,
                            DATEDIFF(MINUTE, t.created_date, GETDATE()) as MinutesAgo,
                            'success' as Type
                        FROM tbl_Transactions t
                        INNER JOIN tbl_Customers c ON t.customer_id = c.customer_id
                        WHERE t.transaction_type = 'Payment' AND t.transaction_status = 'Completed'
                        ORDER BY t.created_date DESC",

                    "Marketing" => @"
                        SELECT TOP (@Limit)
                            'Campaign sent to ' + CAST(total_sent AS VARCHAR) + ' customers' as Text,
                            DATEDIFF(MINUTE, created_date, GETDATE()) as MinutesAgo,
                            'info' as Type
                        FROM tbl_Campaigns
                        WHERE campaign_status = 'Active'
                        ORDER BY created_date DESC",

                    "SalesRep" => @"
                        SELECT TOP (@Limit)
                            'New opportunity created - ₱' + FORMAT(deal_value, 'N0') as Text,
                            DATEDIFF(MINUTE, created_date, GETDATE()) as MinutesAgo,
                            'success' as Type
                        FROM tbl_Sales_Pipeline
                        WHERE assigned_to = @UserId
                        ORDER BY created_date DESC",

                    _ => @"
                        SELECT TOP (@Limit)
                            'New customer registered: ' + first_name + ' ' + last_name as Text,
                            DATEDIFF(MINUTE, created_date, GETDATE()) as MinutesAgo,
                            'success' as Type
                        FROM tbl_Customers
                        WHERE is_archived = 0
                        ORDER BY created_date DESC"
                };

                var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Limit", limit);
                if (userId.HasValue)
                    cmd.Parameters.AddWithValue("@UserId", userId.Value);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var minutesAgo = reader.GetInt32(1);
                        string timeAgo = minutesAgo < 60 ? $"{minutesAgo} min ago" :
                                        minutesAgo < 1440 ? $"{minutesAgo / 60} hour(s) ago" :
                                        $"{minutesAgo / 1440} day(s) ago";

                        activities.Add(new ActivityItem
                        {
                            Text = reader.GetString(0),
                            Time = timeAgo,
                            Type = reader.GetString(2)
                        });
                    }
                }
            }

            return activities;
        }

        // Get Recent Transactions
        public async Task<List<TransactionItem>> GetRecentTransactionsAsync(int limit = 5)
        {
            var transactions = new List<TransactionItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    SELECT TOP (@Limit)
                        i.invoice_number,
                        c.company_name,
                        i.total_amount,
                        i.invoice_date,
                        i.payment_status
                    FROM tbl_Invoices i
                    INNER JOIN tbl_Customers c ON i.customer_id = c.customer_id
                    WHERE i.is_archived = 0
                    ORDER BY i.created_date DESC", connection);
                cmd.Parameters.AddWithValue("@Limit", limit);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        transactions.Add(new TransactionItem
                        {
                            Invoice = reader.GetString(0),
                            Customer = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                            Amount = $"₱{reader.GetDecimal(2):N2}",
                            Date = reader.GetDateTime(3).ToString("MMM dd"),
                            Status = reader.GetString(4)
                        });
                    }
                }
            }

            return transactions;
        }
    }

    // Data Models
    public class AdminDashboardStats
    {
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PeriodRevenue { get; set; }
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class FinanceDashboardStats
    {
        public decimal TotalRevenue { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalTransactions { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal OverdueAmount { get; set; }
    }

    public class MarketingDashboardStats
    {
        public int TotalCustomers { get; set; }
        public int ActiveCampaigns { get; set; }
        public int TotalEmailsSent { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class SalesDashboardStats
    {
        public int TotalOpportunities { get; set; }
        public decimal PipelineValue { get; set; }
        public int ClosedDeals { get; set; }
        public decimal WinRate { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class ActivityItem
    {
        public string Text { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
    }

    public class TransactionItem
    {
        public string Invoice { get; set; }
        public string Customer { get; set; }
        public string Amount { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
    }
}