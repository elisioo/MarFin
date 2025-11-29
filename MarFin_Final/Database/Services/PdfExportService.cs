using System.Text;

namespace MarFin_Final.Database.Services;

public class PdfExportService
{
    /// <summary>
    /// Generates a simple PDF report as HTML that can be printed to PDF
    /// </summary>
    public string GenerateReportHtml(string reportType, DateTime startDate, DateTime endDate, 
        ReportService.CampaignPerformanceData? campaignData = null,
        List<ReportService.RevenueData>? revenueData = null,
        List<ReportService.TopCustomerData>? topCustomers = null,
        ReportService.SalesOpportunitiesData? salesData = null,
        List<ReportService.CustomerSegmentData>? segments = null,
        List<ReportService.RevenueSourceData>? revenueSources = null)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<title>MarFin Report - " + reportType + "</title>");
        html.AppendLine("<style>");
        html.AppendLine(GetCssStyles());
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Header
        html.AppendLine("<div class='header'>");
        html.AppendLine("<h1>MarFin Financial Management System</h1>");
        html.AppendLine("<h2>" + GetReportTitle(reportType) + "</h2>");
        html.AppendLine($"<p>Report Period: {startDate:MMMM dd, yyyy} to {endDate:MMMM dd, yyyy}</p>");
        html.AppendLine($"<p>Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}</p>");
        html.AppendLine("</div>");

        // Report Content
        switch (reportType.ToLower())
        {
            case "financial":
                html.Append(GenerateFinancialReportContent(revenueData, revenueSources));
                break;
            case "campaign":
                html.Append(GenerateCampaignReportContent(campaignData));
                break;
            case "sales":
                html.Append(GenerateSalesReportContent(salesData, topCustomers));
                break;
            case "customer":
                html.Append(GenerateCustomerReportContent(topCustomers, segments));
                break;
            case "invoice":
                html.Append(GenerateInvoiceReportContent());
                break;
            default:
                html.AppendLine("<p>Report type not recognized.</p>");
                break;
        }

        // Footer
        html.AppendLine("<div class='footer'>");
        html.AppendLine("<p>&copy; 2025 MarFin. All rights reserved.</p>");
        html.AppendLine("</div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private string GetReportTitle(string reportType)
    {
        return reportType.ToLower() switch
        {
            "financial" => "Financial Report",
            "campaign" => "Campaign Performance Report",
            "sales" => "Sales Pipeline Report",
            "customer" => "Customer Report",
            "invoice" => "Invoice Report",
            _ => "Report"
        };
    }

    private string GenerateFinancialReportContent(List<ReportService.RevenueData>? revenueData, 
        List<ReportService.RevenueSourceData>? revenueSources)
    {
        var html = new StringBuilder();

        html.AppendLine("<div class='section'>");
        html.AppendLine("<h3>Revenue Overview</h3>");

        if (revenueData != null && revenueData.Any())
        {
            var totalIncome = revenueData.Sum(r => r.Income);
            var totalExpense = revenueData.Sum(r => r.Expense);
            var netProfit = totalIncome - totalExpense;

            html.AppendLine("<div class='metrics-grid'>");
            html.AppendLine($"<div class='metric'><strong>Total Income:</strong> ₱{totalIncome:N2}</div>");
            html.AppendLine($"<div class='metric'><strong>Total Expenses:</strong> ₱{totalExpense:N2}</div>");
            html.AppendLine($"<div class='metric'><strong>Net Profit:</strong> ₱{netProfit:N2}</div>");
            html.AppendLine($"<div class='metric'><strong>Profit Margin:</strong> {(totalIncome > 0 ? (netProfit / totalIncome * 100) : 0):F2}%</div>");
            html.AppendLine("</div>");

            html.AppendLine("<h4>Monthly Breakdown</h4>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Month</th><th>Income</th><th>Expenses</th><th>Net Profit</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var data in revenueData.OrderBy(r => r.Month))
            {
                var monthName = new DateTime(data.Year, data.Month, 1).ToString("MMMM yyyy");
                var profit = data.Income - data.Expense;
                html.AppendLine($"<tr><td>{monthName}</td><td>₱{data.Income:N2}</td><td>₱{data.Expense:N2}</td><td>₱{profit:N2}</td></tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        if (revenueSources != null && revenueSources.Any())
        {
            html.AppendLine("<h4>Revenue by Source</h4>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Source</th><th>Amount</th><th>Percentage</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var source in revenueSources)
            {
                html.AppendLine($"<tr><td>{source.Name}</td><td>₱{source.Amount:N2}</td><td>{source.Percentage}%</td></tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    private string GenerateCampaignReportContent(ReportService.CampaignPerformanceData? campaignData)
    {
        var html = new StringBuilder();

        html.AppendLine("<div class='section'>");
        html.AppendLine("<h3>Campaign Performance Metrics</h3>");

        if (campaignData != null)
        {
            html.AppendLine("<div class='metrics-grid'>");
            html.AppendLine($"<div class='metric'><strong>Email Open Rate:</strong> {campaignData.OpenRate:F2}%</div>");
            html.AppendLine($"<div class='metric'><strong>Click Through Rate:</strong> {campaignData.ClickRate:F2}%</div>");
            html.AppendLine($"<div class='metric'><strong>Conversion Rate:</strong> {campaignData.ConversionRate:F2}%</div>");
            html.AppendLine($"<div class='metric'><strong>ROI:</strong> {campaignData.ROI:F2}%</div>");
            html.AppendLine("</div>");

            html.AppendLine("<h4>Performance Analysis</h4>");
            html.AppendLine("<ul>");
            html.AppendLine($"<li>Email campaigns achieved an average open rate of <strong>{campaignData.OpenRate:F2}%</strong>, indicating good subject line effectiveness.</li>");
            html.AppendLine($"<li>Click-through rate of <strong>{campaignData.ClickRate:F2}%</strong> shows engagement with campaign content.</li>");
            html.AppendLine($"<li>Conversion rate of <strong>{campaignData.ConversionRate:F2}%</strong> reflects the effectiveness of call-to-action elements.</li>");
            html.AppendLine($"<li>Overall ROI of <strong>{campaignData.ROI:F2}%</strong> demonstrates strong campaign profitability.</li>");
            html.AppendLine("</ul>");
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    private string GenerateSalesReportContent(ReportService.SalesOpportunitiesData? salesData, 
        List<ReportService.TopCustomerData>? topCustomers)
    {
        var html = new StringBuilder();

        html.AppendLine("<div class='section'>");
        html.AppendLine("<h3>Sales Pipeline Analysis</h3>");

        if (salesData != null)
        {
            html.AppendLine("<div class='metrics-grid'>");
            html.AppendLine($"<div class='metric'><strong>Active Opportunities:</strong> {salesData.Active}</div>");
            html.AppendLine($"<div class='metric'><strong>Won Deals:</strong> {salesData.Won}</div>");
            html.AppendLine($"<div class='metric'><strong>Lost Deals:</strong> {salesData.Lost}</div>");
            html.AppendLine($"<div class='metric'><strong>Win Rate:</strong> {salesData.WinRate:F2}%</div>");
            html.AppendLine($"<div class='metric'><strong>Total Pipeline Value:</strong> ₱{salesData.TotalPipelineValue:N2}</div>");
            html.AppendLine("</div>");
        }

        if (topCustomers != null && topCustomers.Any())
        {
            html.AppendLine("<h4>Top Customers</h4>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Rank</th><th>Customer Name</th><th>Total Amount</th><th>Transactions</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var customer in topCustomers)
            {
                html.AppendLine($"<tr><td>{customer.Rank}</td><td>{customer.Name}</td><td>₱{customer.Amount:N2}</td><td>{customer.Transactions}</td></tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    private string GenerateCustomerReportContent(List<ReportService.TopCustomerData>? topCustomers, 
        List<ReportService.CustomerSegmentData>? segments)
    {
        var html = new StringBuilder();

        html.AppendLine("<div class='section'>");
        html.AppendLine("<h3>Customer Analysis</h3>");

        if (topCustomers != null && topCustomers.Any())
        {
            html.AppendLine("<h4>Top Customers</h4>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Rank</th><th>Customer Name</th><th>Total Amount</th><th>Transactions</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var customer in topCustomers)
            {
                html.AppendLine($"<tr><td>{customer.Rank}</td><td>{customer.Name}</td><td>₱{customer.Amount:N2}</td><td>{customer.Transactions}</td></tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        if (segments != null && segments.Any())
        {
            html.AppendLine("<h4>Customer Segments</h4>");
            html.AppendLine("<table>");
            html.AppendLine("<thead><tr><th>Segment</th><th>Count</th><th>Percentage</th></tr></thead>");
            html.AppendLine("<tbody>");

            foreach (var segment in segments)
            {
                html.AppendLine($"<tr><td>{segment.Name}</td><td>{segment.Count}</td><td>{segment.Percentage}%</td></tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    private string GenerateInvoiceReportContent()
    {
        var html = new StringBuilder();

        html.AppendLine("<div class='section'>");
        html.AppendLine("<h3>Invoice Summary</h3>");
        html.AppendLine("<p>Invoice report data will be displayed here with payment status and aging analysis.</p>");
        html.AppendLine("</div>");

        return html.ToString();
    }

    private string GetCssStyles()
    {
        return @"
            * {
                margin: 0;
                padding: 0;
                box-sizing: border-box;
            }

            body {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                color: #333;
                line-height: 1.6;
                background-color: #f5f5f5;
            }

            .header {
                background-color: #0A1828;
                color: white;
                padding: 40px;
                text-align: center;
                margin-bottom: 40px;
                border-radius: 8px;
            }

            .header h1 {
                font-size: 28px;
                margin-bottom: 10px;
            }

            .header h2 {
                font-size: 20px;
                margin-bottom: 15px;
                font-weight: 600;
            }

            .header p {
                font-size: 14px;
                opacity: 0.9;
            }

            .section {
                background-color: white;
                padding: 30px;
                margin-bottom: 30px;
                border-radius: 8px;
                box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
            }

            .section h3 {
                font-size: 20px;
                color: #0A1828;
                margin-bottom: 20px;
                border-bottom: 2px solid #0A1828;
                padding-bottom: 10px;
            }

            .section h4 {
                font-size: 16px;
                color: #333;
                margin-top: 20px;
                margin-bottom: 15px;
            }

            .metrics-grid {
                display: grid;
                grid-template-columns: repeat(2, 1fr);
                gap: 20px;
                margin-bottom: 20px;
            }

            .metric {
                padding: 15px;
                background-color: #f9fafb;
                border-left: 4px solid #3b82f6;
                border-radius: 4px;
            }

            .metric strong {
                color: #0A1828;
                display: block;
                margin-bottom: 5px;
            }

            table {
                width: 100%;
                border-collapse: collapse;
                margin-top: 15px;
            }

            table thead {
                background-color: #f0f0f0;
            }

            table th {
                padding: 12px;
                text-align: left;
                font-weight: 600;
                color: #333;
                border-bottom: 2px solid #ddd;
            }

            table td {
                padding: 12px;
                border-bottom: 1px solid #eee;
            }

            table tbody tr:nth-child(even) {
                background-color: #f9fafb;
            }

            ul {
                margin-left: 20px;
                margin-top: 15px;
            }

            ul li {
                margin-bottom: 10px;
                line-height: 1.8;
            }

            .footer {
                text-align: center;
                padding: 20px;
                color: #666;
                font-size: 12px;
                border-top: 1px solid #eee;
                margin-top: 40px;
            }

            @media print {
                body {
                    background-color: white;
                }
                .section {
                    page-break-inside: avoid;
                    box-shadow: none;
                    border: 1px solid #eee;
                }
            }
        ";
    }
}
