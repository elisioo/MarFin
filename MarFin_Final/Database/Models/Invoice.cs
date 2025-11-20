// Models/Invoice.cs
using System;
using System.Collections.Generic;

namespace MarFin.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public int CreatedBy { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public string? PdfPath { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Navigation properties
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public List<InvoiceItem> Items { get; set; } = new();
    }

    public class InvoiceItem
    {
        public int ItemId { get; set; }
        public int InvoiceId { get; set; }
        public int ItemOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceFilterRequest
    {
        public string? SearchTerm { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SortBy { get; set; } = "invoice_date";
        public string? SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? MaxRecords { get; set; } = 200;
    }

    public class InvoiceListResponse
    {
        public List<InvoiceListItem> Invoices { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
        public FinancialSummary Summary { get; set; } = new();
    }

    public class InvoiceListItem
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCompany { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
    }

    public class FinancialSummary
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalOverdue { get; set; }
        public int PendingInvoicesCount { get; set; }
        public int OverdueInvoicesCount { get; set; }
        public decimal RevenueChangePercent { get; set; }
    }

    public class CreateInvoiceRequest
    {
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? PaymentTerms { get; set; }
        public string? Notes { get; set; }
        public decimal TaxRate { get; set; } = 12.00m;

        // FIX: This is now NON-NULLABLE and defaults to 0
        public decimal DiscountAmount { get; set; } = 0m;

        public List<InvoiceItemRequest> Items { get; set; } = new()
    {
        new InvoiceItemRequest { Description = "", Quantity = 1, UnitPrice = 0 }
    };
    }
    public class InvoiceItemRequest
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdateInvoiceStatusRequest
    {
        public int InvoiceId { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}