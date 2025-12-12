using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarFin_Final.Models
{
    public class Invoice
    {
        [Key]
        [Column("invoice_id")]
        public int InvoiceId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("created_by")]
        public int CreatedBy { get; set; }

        [Required]
        [StringLength(50)]
        [Column("invoice_number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Column("invoice_date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Column("due_date")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);

        [StringLength(50)]
        [Column("payment_terms")]
        public string PaymentTerms { get; set; } = "Net 30";

        [Column("subtotal", TypeName = "decimal(12, 2)")]
        public decimal? Subtotal { get; set; } = 0m;

        [Column("tax_rate", TypeName = "decimal(5, 2)")]
        public decimal? TaxRate { get; set; } = 12.00m;

        [Column("tax_amount", TypeName = "decimal(12, 2)")]
        public decimal? TaxAmount { get; set; } = 0m;

        [Column("discount_amount", TypeName = "decimal(12, 2)")]
        public decimal? DiscountAmount { get; set; } = 0m;

        [Column("total_amount", TypeName = "decimal(12, 2)")]
        public decimal? TotalAmount { get; set; } = 0m;

        [StringLength(20)]
        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Draft";

        [Column("notes")]
        public string? Notes { get; set; } = string.Empty;

        [Column("pdf_path")]
        public string? PdfPath { get; set; } = string.Empty;

        [Column("is_archived")]
        public bool IsArchived { get; set; } = false;

        [Column("archived_date")]
        public DateTime? ArchivedDate { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column("modified_date")]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Display-only properties (not mapped to DB)
        [NotMapped]
        public string CustomerName { get; set; } = string.Empty;

        [NotMapped]
        public string CustomerEmail { get; set; } = string.Empty;

        [NotMapped]
        public string CustomerCompany { get; set; } = string.Empty;

        [NotMapped]
        public string CreatedByName { get; set; } = string.Empty;

        public List<InvoiceItem> LineItems { get; set; } = [];

        [NotMapped]
        public bool IsOverdue => PaymentStatus != "Paid" && PaymentStatus != "Void" && DueDate < DateTime.Now;

        [NotMapped]
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
        [Key]
        [Column("item_id")]
        public int ItemId { get; set; }

        [Column("invoice_id")]
        public int InvoiceId { get; set; }

        [Column("item_order")]
        public int ItemOrder { get; set; } = 1;

        [Required]
        [StringLength(200)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("quantity", TypeName = "decimal(10, 2)")]
        public decimal? Quantity { get; set; } = 1m;

        [Column("unit_price", TypeName = "decimal(12, 2)")]
        public decimal? UnitPrice { get; set; } = 0m;

        [Column("amount", TypeName = "decimal(12, 2)")]
        public decimal? Amount { get; set; } = 0m;

        public Invoice Invoice { get; set; } = null!;

        [NotMapped]
        public decimal CalculatedAmount => (Quantity ?? 0m) * (UnitPrice ?? 0m);
    }
}