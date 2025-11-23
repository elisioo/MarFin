using System;
using System.ComponentModel.DataAnnotations;

namespace MarFin.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        public int? InvoiceId { get; set; }

        [Required(ErrorMessage = "Recorded by user is required")]
        public int RecordedBy { get; set; }

        [Required(ErrorMessage = "Transaction date is required")]
        public DateTime TransactionDate { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(20)]
        public string TransactionType { get; set; } = "Payment";

        [StringLength(20)]
        public string TransactionStatus { get; set; } = "Completed";

        public string? Notes { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime? ArchivedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public string? CustomerName { get; set; }
        public string? CustomerCompany { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? RecordedByName { get; set; }
    }

    public class TransactionListItem
    {
        public int TransactionId { get; set; }
        public int CustomerId { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReferenceNumber { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionStatus { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerCompany { get; set; }
        public string? InvoiceNumber { get; set; }
        public string RecordedByName { get; set; } = string.Empty;
    }

    public class TransactionSummary
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int FailedTransactions { get; set; }
    }

    public class TransactionFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TransactionType { get; set; }
        public string? TransactionStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public int? CustomerId { get; set; }
        public string? SearchTerm { get; set; }
        public string SortBy { get; set; } = "transaction_date_desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}