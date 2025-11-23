    using System;
    using System.Collections.Generic;

    namespace MarFin_Final.Models
    {
        public class Invoice
        {
            // Primary Key
            public int InvoiceId { get; set; }

            // Foreign Keys
            public int CustomerId { get; set; }
            public int CreatedBy { get; set; }

            // Invoice Information
            public string InvoiceNumber { get; set; } = "";
            public DateTime InvoiceDate { get; set; } = DateTime.Now;
            public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);
            public string PaymentTerms { get; set; } = "Net 30";

            // Financial Information
            public decimal Subtotal { get; set; } = 0.00m;
            public decimal TaxRate { get; set; } = 12.00m;
            public decimal TaxAmount { get; set; } = 0.00m;
            public decimal DiscountAmount { get; set; } = 0.00m;
            public decimal TotalAmount { get; set; } = 0.00m;

            // Status and Additional Information
            public string PaymentStatus { get; set; } = "Draft";
            public string Notes { get; set; } = "";
            public string PdfPath { get; set; } = "";

            // System Fields
            public bool IsArchived { get; set; } = false;
            public DateTime? ArchivedDate { get; set; }
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public DateTime ModifiedDate { get; set; } = DateTime.Now;

            // Navigation/Display Properties (populated from JOINs)
            public string CustomerName { get; set; } = "";
            public string CustomerEmail { get; set; } = "";
            public string CustomerCompany { get; set; } = "";
            public string CreatedByName { get; set; } = "";

            // Line Items
            public List<InvoiceItem> LineItems { get; set; } = new();

            // Computed Properties
            public bool IsOverdue => PaymentStatus != "Paid" && PaymentStatus != "Void" && DueDate < DateTime.Now;
            public int DaysUntilDue => (DueDate - DateTime.Now).Days;
            public string StatusBadgeClass => PaymentStatus?.ToLower() switch
            {
                "paid" => "status-paid",
                "issued" => "status-issued",
                "partial" => "status-partial",
                "overdue" => "status-overdue",
                "void" => "status-void",
                _ => "status-draft"
            };
        }

        public class InvoiceItem
        {
            // Primary Key
            public int ItemId { get; set; }

            // Foreign Key
            public int InvoiceId { get; set; }

            // Item Information
            public int ItemOrder { get; set; } = 1;
            public string Description { get; set; } = "";
            public decimal Quantity { get; set; } = 1.00m;
            public decimal UnitPrice { get; set; } = 0.00m;
            public decimal Amount { get; set; } = 0.00m;

            // Computed Property
            public decimal CalculatedAmount => Quantity * UnitPrice;
        }
    }