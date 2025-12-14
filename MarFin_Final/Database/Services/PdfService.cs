using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MarFin_Final.Models;

namespace MarFin_Final.Services
{
    public static class PdfService
    {
        public static byte[] GenerateInvoicePdf(Invoice invoice)
        {
            QuestPDF.Settings.License = LicenseType.Community; // Free for commercial use

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("MarFin").FontSize(20).Bold().FontColor("#1a5fb4");
                                    col.Item().Text("123 Business Street");
                                    col.Item().Text("Makati, Manila Philippines 1200");
                                    col.Item().Text("contact@marfin.com");
                                    col.Item().Text("+63 912 345 6789");
                                });

                                row.ConstantItem(150).AlignRight().Column(col =>
                                {
                                    col.Item().AlignCenter().PaddingBottom(10)
                                        .Text("INVOICE").FontSize(28).Bold().FontColor("#1a5fb4");
                                    col.Item().AlignCenter()
                                        .Text($"#{invoice.InvoiceNumber}").FontSize(18);
                                });
                            });

                            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Bill To:").Bold();
                                    col.Item().Text(invoice.CustomerCompany ?? invoice.CustomerName).Bold();
                                    col.Item().Text(invoice.CustomerName);
                                    col.Item().Text(invoice.CustomerEmail);
                                });

                                row.ConstantItem(200).AlignRight().Column(col =>
                                {
                                    col.Item().Text("Invoice Date:").SemiBold();
                                    col.Item().Text(invoice.InvoiceDate.ToString("MMMM dd, yyyy"));
                                    col.Spacing(5);

                                    col.Item().Text("Due Date:").SemiBold();
                                    col.Item().Text(invoice.DueDate.ToString("MMMM dd, yyyy"));
                                    col.Spacing(5);

                                    col.Item().Text("Payment Terms:").SemiBold();
                                    col.Item().Text(invoice.PaymentTerms ?? "Net 30");
                                });
                            });

                            column.Item().PaddingTop(30).Table(table =>
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

                                    header.Cell().ColumnSpan(4).PaddingTop(5).LineHorizontal(1);
                                });

                                foreach (var item in invoice.LineItems)
                                {
                                    table.Cell().Text(item.Description ?? "");
                                    table.Cell().AlignRight().Text(item.Quantity?.ToString("N0") ?? "0");
                                    table.Cell().AlignRight().Text($"Php {(item.UnitPrice ?? 0m):N2}");
                                    table.Cell().AlignRight().Text($"Php {(item.Amount ?? 0m):N2}");
                                }
                            });

                            column.Item().AlignRight().PaddingTop(20).Column(col =>
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Subtotal:");
                                    row.ConstantItem(120).AlignRight().Text($"Php {(invoice.Subtotal ?? 0m):N2}");
                                });

                                if (invoice.TaxAmount > 0)
                                {
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"Tax ({invoice.TaxRate}%)");
                                        row.ConstantItem(120).AlignRight().Text($"Php {(invoice.TaxAmount ?? 0m):N2}");
                                    });
                                }

                                if (invoice.DiscountAmount > 0)
                                {
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text("Discount:");
                                        row.ConstantItem(120).AlignRight().Text($"-Php {(invoice.DiscountAmount ?? 0m):N2}");
                                    });
                                }

                                col.Item().PaddingTop(10).BorderTop(2).BorderColor("#1a5fb4").Row(row =>
                                {
                                    row.RelativeItem().Text("Total Amount Due:").Bold().FontSize(16);
                                    row.ConstantItem(150).AlignRight().Text($"Php {(invoice.TotalAmount ?? 0m):N2}")
                                        .Bold().FontSize(18).FontColor("#1a5fb4");
                                });
                            });

                            column.Item().PaddingTop(40).Column(col =>
                            {
                                col.Item().Text("PAYMENT INFORMATION").Bold().FontSize(14);
                                col.Item().Text("Bank Name: BPI (Bank of the Philippine Islands)");
                                col.Item().Text("Account Number: 1234-5678-9012-3456");
                                col.Item().Text("Account Name: MarFin Business Account");
                            });

                            if (!string.IsNullOrEmpty(invoice.Notes))
                            {
                                column.Item().PaddingTop(30).Column(col =>
                                {
                                    col.Item().Text("Notes:").Bold();
                                    col.Item().Text(invoice.Notes);
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            })
            .GeneratePdf();
        }
    }
}