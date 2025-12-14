// Services/InvoicePdfService.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MarFin_Final.Models;

namespace MarFin_Final.Services
{
    public class InvoicePdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Segoe UI"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("MarFin").FontSize(24).Bold().FontColor("#1a73e8");
                            col.Item().Text("123 Business Street").FontSize(10);
                            col.Item().Text("Makati, Manila Philippines 1200");
                            col.Item().Text("contact@marfin.com | +63 912 345 6789");
                        });

                        row.ConstantItem(150).AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text("INVOICE").FontSize(28).Bold().FontColor("#1a73e8");
                            col.Item().AlignRight().Text($"#{invoice.InvoiceNumber}").FontSize(16);
                        });
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Bill To:").Bold();
                                c.Item().Text(string.IsNullOrEmpty(invoice.CustomerCompany)
                                    ? invoice.CustomerName
                                    : invoice.CustomerCompany).Bold();
                                if (!string.IsNullOrEmpty(invoice.CustomerCompany))
                                    c.Item().Text(invoice.CustomerName);
                                c.Item().Text(invoice.CustomerEmail ?? "");
                            });

                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text("Invoice Date:").FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                c.Item().Text(invoice.InvoiceDate.ToString("MM/dd/yyyy")).Bold();

                                c.Item().PaddingTop(10).Text("Due Date:").FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                c.Item().Text(invoice.DueDate.ToString("MM/dd/yyyy")).Bold();

                                c.Item().PaddingTop(10).Text("Payment Terms:").FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                c.Item().Text(invoice.PaymentTerms ?? "Net 30");
                            });
                        });

                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Description").Bold();
                                header.Cell().AlignRight().Text("Qty").Bold();
                                header.Cell().AlignRight().Text("Unit Price").Bold();
                                header.Cell().AlignRight().Text("Amount").Bold();

                                header.Cell().ColumnSpan(4).PaddingTop(5).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                            });

                            foreach (var item in invoice.LineItems.OrderBy(i => i.ItemOrder))
                            {
                                table.Cell().Text(item.Description ?? "");
                                table.Cell().AlignRight().Text($"{item.Quantity:F2}");
                                table.Cell().AlignRight().Text($"Php {(item.UnitPrice ?? 0):N2}");
                                table.Cell().AlignRight().Text($"Php {(item.Amount ?? 0):N2}");
                            }
                        });

                        col.Item().AlignRight().PaddingTop(20).Column(summary =>
                        {
                            summary.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Subtotal");
                                row.ConstantItem(120).AlignRight().Text($"Php {(invoice.Subtotal ?? 0):N2}");
                            });

                            if (invoice.TaxAmount > 0)
                            {
                                summary.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Tax ({invoice.TaxRate:F2}%)");
                                    row.ConstantItem(120).AlignRight().Text($"Php {(invoice.TaxAmount ?? 0):N2}");
                                });
                            }

                            if (invoice.DiscountAmount > 0)
                            {
                                summary.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Discount");
                                    row.ConstantItem(120).AlignRight().Text($"-Php {(invoice.DiscountAmount ?? 0):N2}");
                                });
                            }

                            summary.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("Total").Bold().FontSize(16);
                                row.ConstantItem(120).AlignRight().Text($"Php {(invoice.TotalAmount ?? 0):N2}").Bold().FontSize(18).FontColor("#1a73e8");
                            });
                        });

                        col.Item().PaddingTop(40).Column(col =>
                        {
                            col.Item().Text("PAYMENT INFORMATION").Bold().FontSize(14);
                            col.Item().Text("Bank Name: BPI (Bank of the Philippine Islands)");
                            col.Item().Text("Account Number: 1234-5678-9012-3456");
                            col.Item().Text("Account Name: MarFin Business Account");
                        });

                        if (!string.IsNullOrEmpty(invoice.Notes))
                        {
                            col.Item().PaddingTop(30).Column(notes =>
                            {
                                notes.Item().Text("Notes:").Bold();
                                notes.Item().Text(invoice.Notes).FontSize(10);
                            });
                        }

                        var statusColor = invoice.PaymentStatus switch
                        {
                            "Paid" => QuestPDF.Helpers.Colors.Green.Medium,
                            "Partial" => QuestPDF.Helpers.Colors.Orange.Medium,
                            "Overdue" => QuestPDF.Helpers.Colors.Red.Medium,
                            _ => QuestPDF.Helpers.Colors.Blue.Medium
                        };

                        var statusText = invoice.IsOverdue ? "OVERDUE" : invoice.PaymentStatus?.ToUpper() ?? "DRAFT";

                        page.Footer().AlignRight().PaddingTop(20).Text(statusText)
                            .FontSize(24).SemiBold().FontColor(statusColor);
                    });
                });
            }).GeneratePdf();
        }
    }
}