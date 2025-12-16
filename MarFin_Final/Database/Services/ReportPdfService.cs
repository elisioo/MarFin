using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MarFin_Final.Database.Services;
using MarFin_Final.Models;
using Colors = QuestPDF.Helpers.Colors;

namespace MarFin_Final.Services;

public static class ReportPdfService
{
    static ReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateReportPdf(
        string reportType,
        DateTime startDate,
        DateTime endDate,
        ReportService.CampaignPerformanceData? campaignData = null,
        List<ReportService.RevenueData>? revenueData = null,
        List<ReportService.TopCustomerData>? topCustomers = null,
        ReportService.SalesOpportunitiesData? salesData = null,
        List<ReportService.CustomerSegmentData>? segments = null,
        List<ReportService.RevenueSourceData>? revenueSources = null,
        List<Invoice>? invoices = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                // Header
                page.Header().Background("#0A1828").Padding(20).Column(col =>
                {
                    col.Item().Text("MarFin Financial Management System")
                        .FontSize(22).Bold().FontColor(Colors.White);

                    col.Item().Text(GetReportTitle(reportType))
                        .FontSize(18).SemiBold().FontColor(Colors.White);

                    col.Item().Text($"Report Period: {startDate:MMMM dd, yyyy} – {endDate:MMMM dd, yyyy}")
                        .FontSize(12).FontColor("#E0E0E0");

                    col.Item().Text($"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}")
                        .FontSize(10).FontColor("#CCCCCC");
                });

                // Content
                page.Content().PaddingVertical(30).Column(column =>
                {
                    switch (reportType.ToLower())
                    {
                        case "financial":
                            GenerateFinancialReport(column, revenueData, revenueSources);
                            break;
                        case "campaign":
                            GenerateCampaignReport(column, campaignData);
                            break;
                        case "sales":
                            GenerateSalesReport(column, salesData, topCustomers);
                            break;
                        case "customer":
                            GenerateCustomerReport(column, topCustomers, segments);
                            break;
                        case "invoice":
                            GenerateInvoiceReport(column, invoices);
                            break;
                        case "all":
                            GenerateCombinedReport(column, revenueData, revenueSources, campaignData, salesData, topCustomers, segments);
                            break;
                        default:
                            column.Item().Text("Unknown report type selected.")
                                .FontColor(Colors.Red.Medium).Italic();
                            break;
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    text.Span("  •  2025 MarFin. All rights reserved.").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }


    private static void GenerateCombinedReport(
    ColumnDescriptor column,
    List<ReportService.RevenueData>? revenueData,
    List<ReportService.RevenueSourceData>? revenueSources,
    ReportService.CampaignPerformanceData? campaignData,
    ReportService.SalesOpportunitiesData? salesData,
    List<ReportService.TopCustomerData>? topCustomers,
    List<ReportService.CustomerSegmentData>? segments)
    {
        column.Item().Text("Comprehensive Analytics Report").FontSize(18).Bold().LetterSpacing(20);

        // Call each individual section in sequence
        GenerateFinancialReport(column, revenueData, revenueSources);
        column.Item().PageBreak(); // Optional: force new page between sections

        GenerateCampaignReport(column, campaignData);
        column.Item().PageBreak();

        GenerateSalesReport(column, salesData, topCustomers);
        column.Item().PageBreak();

        GenerateCustomerReport(column, topCustomers, segments);
    }

    private static string GetReportTitle(string type) => type.ToLower() switch
    {
        "financial" => "Financial Performance Report",
        "campaign" => "Marketing Campaign Performance Report",
        "sales" => "Sales Pipeline Report",
        "customer" => "Customer Analysis Report",
        "invoice" => "Invoice Summary Report",
        "all" => "Comprehensive Analytics Report",
        _ => "Custom Report"
    };

    // ==================== FINANCIAL REPORT ====================
    private static void GenerateFinancialReport(ColumnDescriptor column,
        List<ReportService.RevenueData>? revenueData,
        List<ReportService.RevenueSourceData>? revenueSources)
    {
        if (revenueData == null || revenueData.Count == 0)
        {
            column.Item().Text("No revenue data available for the selected period.").Italic();
            return;
        }

        var totalIncome = revenueData.Sum(r => r.Income);
        var totalExpense = revenueData.Sum(r => r.Expense);
        var netProfit = totalIncome - totalExpense;

        // Summary Metrics
        column.Item().PaddingBottom(20).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(); cols.RelativeColumn();
                cols.RelativeColumn(); cols.RelativeColumn();
            });

            table.Cell().ColumnSpan(4).Text("Revenue Overview").FontSize(16).Bold();

            table.Cell().Text("Total Income").SemiBold();
            table.Cell().AlignRight().Text($"₱{totalIncome:N2}").Bold();

            table.Cell().Text("Total Expenses").SemiBold();
            table.Cell().AlignRight().Text($"₱{totalExpense:N2}").Bold();

            table.Cell().Text("Net Profit").SemiBold();
            table.Cell().AlignRight().Text($"₱{netProfit:N2}")
                .Bold().FontColor(netProfit >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken3);

            table.Cell().Text("Profit Margin").SemiBold();
            table.Cell().AlignRight().Text($"{(totalIncome > 0 ? netProfit / totalIncome * 100 : 0):F2}%").Bold();
        });

        // Monthly Breakdown
        column.Item().PaddingTop(30).Text("Monthly Revenue Breakdown").FontSize(14).Bold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(120);
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Text("Month").Bold();
                header.Cell().Text("Income").Bold().AlignRight();
                header.Cell().Text("Expenses").Bold().AlignRight();
                header.Cell().Text("Net Profit").Bold().AlignRight();
            });

            foreach (var r in revenueData.OrderBy(x => x.Year).ThenBy(x => x.Month))
            {
                var monthName = new DateTime(r.Year, r.Month, 1).ToString("MMMM yyyy");
                var profit = r.Income - r.Expense;

                table.Cell().Text(monthName);
                table.Cell().AlignRight().Text($"₱{r.Income:N2}");
                table.Cell().AlignRight().Text($"₱{r.Expense:N2}");
                table.Cell().AlignRight().Text($"₱{profit:N2}")
                    .FontColor(profit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
            }
        });

        // Revenue by Source
        if (revenueSources != null && revenueSources.Count > 0)
        {
            column.Item().PaddingTop(30).Text("Revenue by Source").FontSize(14).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Source").Bold();
                    header.Cell().Text("Amount").Bold().AlignRight();
                    header.Cell().Text("Percentage").Bold().AlignRight();
                });

                foreach (var s in revenueSources)
                {
                    table.Cell().Text(s.Name ?? "Unknown");
                    table.Cell().AlignRight().Text($"₱{s.Amount:N2}");
                    table.Cell().AlignRight().Text($"{s.Percentage:F2}%");
                }
            });
        }
    }

    // ==================== CAMPAIGN REPORT ====================
    private static void GenerateCampaignReport(ColumnDescriptor column, ReportService.CampaignPerformanceData? data)
    {
        if (data == null)
        {
            column.Item().Text("No campaign performance data available.").Italic();
            return;
        }

        column.Item().Text("Key Campaign Performance Metrics").FontSize(16).Bold();
        column.Item().PaddingVertical(20).Table(table =>
        {
            table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });

            table.Cell().Text("Email Open Rate").SemiBold();
            table.Cell().AlignRight().Text($"{data.OpenRate:F2}%").Bold().FontSize(14);

            table.Cell().Text("Click-Through Rate").SemiBold();
            table.Cell().AlignRight().Text($"{data.ClickRate:F2}%").Bold().FontSize(14);

            table.Cell().Text("Conversion Rate").SemiBold();
            table.Cell().AlignRight().Text($"{data.ConversionRate:F2}%").Bold().FontSize(14);

            table.Cell().Text("Return on Investment (ROI)").SemiBold();
            table.Cell().AlignRight().Text($"{data.ROI:F2}%")
                .Bold().FontSize(14)
                .FontColor(data.ROI >= 100 ? Colors.Green.Darken3 : Colors.Orange.Darken3);
        });
    }

    // ==================== SALES REPORT ====================
    private static void GenerateSalesReport(ColumnDescriptor column,
        ReportService.SalesOpportunitiesData? salesData,
        List<ReportService.TopCustomerData>? topCustomers)
    {
        if (salesData != null)
        {
            column.Item().Text("Sales Pipeline Summary").FontSize(16).Bold();
            column.Item().PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(); cols.RelativeColumn();
                    cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn();
                });

                table.Cell().Text("Active").SemiBold().AlignCenter();
                table.Cell().Text("Won").SemiBold().AlignCenter();
                table.Cell().Text("Lost").SemiBold().AlignCenter();
                table.Cell().Text("Total Pipeline Value").SemiBold().AlignCenter();
                table.Cell().Text("Win Rate").SemiBold().AlignCenter();

                table.Cell().AlignCenter().Text(salesData.Active.ToString("N0"));
                table.Cell().AlignCenter().Text(salesData.Won.ToString("N0"));
                table.Cell().AlignCenter().Text(salesData.Lost.ToString("N0"));
                table.Cell().AlignCenter().Text($"₱{salesData.TotalPipelineValue:N2}");
                table.Cell().AlignCenter().Text($"{salesData.WinRate:F2}%").Bold();
            });
        }

        if (topCustomers != null && topCustomers.Count > 0)
        {
            column.Item().PaddingTop(30).Text("Top Customers by Revenue").FontSize(14).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(60);
                    cols.RelativeColumn(3);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Rank").Bold();
                    header.Cell().Text("Customer Name").Bold();
                    header.Cell().Text("Total Amount").Bold().AlignRight();
                    header.Cell().Text("Transactions").Bold().AlignRight();
                });

                foreach (var c in topCustomers)
                {
                    table.Cell().Text(c.Rank.ToString());
                    table.Cell().Text(c.Name ?? "Unknown Customer");
                    table.Cell().AlignRight().Text($"₱{c.Amount:N2}");
                    table.Cell().AlignRight().Text(c.Transactions.ToString("N0"));
                }
            });
        }
    }

    // ==================== CUSTOMER REPORT ====================
    private static void GenerateCustomerReport(ColumnDescriptor column,
        List<ReportService.TopCustomerData>? topCustomers,
        List<ReportService.CustomerSegmentData>? segments)
    {
        if (topCustomers != null && topCustomers.Count > 0)
        {
            column.Item().Text("Top Customers by Revenue").FontSize(14).Bold();
            column.Item().PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(60);
                    cols.RelativeColumn(3);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Rank").Bold();
                    header.Cell().Text("Customer Name").Bold();
                    header.Cell().Text("Total Amount").Bold().AlignRight();
                    header.Cell().Text("Transactions").Bold().AlignRight();
                });

                foreach (var c in topCustomers)
                {
                    table.Cell().Text(c.Rank.ToString());
                    table.Cell().Text(c.Name ?? "Unknown Customer");
                    table.Cell().AlignRight().Text($"₱{c.Amount:N2}");
                    table.Cell().AlignRight().Text(c.Transactions.ToString("N0"));
                }
            });
        }

        if (segments != null && segments.Count > 0)
        {
            column.Item().PaddingTop(30).Text("Customer Segments Distribution").FontSize(14).Bold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2);
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Text("Segment").Bold();
                    header.Cell().Text("Customer Count").Bold().AlignRight();
                    header.Cell().Text("Percentage").Bold().AlignRight();
                });

                foreach (var s in segments)
                {
                    table.Cell().Text(s.Name ?? "Unknown");
                    table.Cell().AlignRight().Text(s.Count.ToString("N0"));
                    table.Cell().AlignRight().Text($"{s.Percentage:F2}%");
                }
            });
        }
    }

    // ==================== INVOICE REPORT ====================
    private static void GenerateInvoiceReport(ColumnDescriptor column, List<Invoice>? invoices)
    {
        if (invoices == null || invoices.Count == 0)
        {
            column.Item().Text("No invoices found for the selected period.").Italic();
            return;
        }

        var total = invoices.Sum(i => i.TotalAmount);
        var paid = invoices.Where(i => i.PaymentStatus?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true)
                          .Sum(i => i.TotalAmount);
        var overdue = invoices.Where(i => i.IsOverdue).Sum(i => i.TotalAmount);

        column.Item().Text("Invoice Summary").FontSize(16).Bold();
        column.Item().PaddingBottom(20).Table(table =>
        {
            table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });

            table.Cell().Text("Total Invoices").SemiBold();
            table.Cell().AlignRight().Text(invoices.Count.ToString("N0")).Bold();

            table.Cell().Text("Total Amount").SemiBold();
            table.Cell().AlignRight().Text($"₱{total:N2}").Bold();

            table.Cell().Text("Paid Amount").SemiBold();
            table.Cell().AlignRight().Text($"₱{paid:N2}").Bold().FontColor(Colors.Green.Darken3);

            table.Cell().Text("Overdue Amount").SemiBold();
            table.Cell().AlignRight().Text($"₱{overdue:N2}").Bold().FontColor(Colors.Red.Darken3);
        });

        column.Item().PaddingTop(20).Text("Detailed Invoice List").FontSize(14).Bold();
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(110);  // Invoice #
                cols.ConstantColumn(90);   // Date
                cols.RelativeColumn(2);    // Customer
                cols.ConstantColumn(80);   // Status
                cols.ConstantColumn(90);   // Due Date
                cols.RelativeColumn();     // Amount
            });

            table.Header(header =>
            {
                header.Cell().Text("Invoice #").Bold();
                header.Cell().Text("Date").Bold();
                header.Cell().Text("Customer").Bold();
                header.Cell().Text("Status").Bold();
                header.Cell().Text("Due Date").Bold();
                header.Cell().Text("Amount").Bold().AlignRight();
            });

            foreach (var inv in invoices.OrderByDescending(i => i.InvoiceDate))
            {
                var status = inv.IsOverdue && !string.Equals(inv.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                    ? "Overdue"
                    : inv.PaymentStatus ?? "Pending";

                var customerDisplay = string.IsNullOrEmpty(inv.CustomerCompany)
                    ? inv.CustomerName
                    : inv.CustomerCompany;

                table.Cell().Text(inv.InvoiceNumber ?? "N/A");
                table.Cell().Text(inv.InvoiceDate.ToString("MMM dd, yyyy"));
                table.Cell().Text(customerDisplay ?? "Unknown");
                table.Cell().Text(status).FontColor(status == "Overdue" ? Colors.Red.Medium : Colors.Black);
                table.Cell().Text(inv.DueDate.ToString("MMM dd, yyyy"));
                table.Cell().AlignRight().Text($"₱{inv.TotalAmount:N2}");
            }
        });
    }
}