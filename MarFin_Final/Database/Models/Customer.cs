using System;

namespace Ordering_Sorono_IT13.Models
{
    public class Customer
    {
        // Primary Key
        public int CustomerId { get; set; }

        // Foreign Keys
        public int SegmentId { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }

        // Personal Information
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // Company Information
        public string CompanyName { get; set; } = "";

        // Address Information
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string StateProvince { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Country { get; set; } = "Philippines";

        // Status and Business Information
        public string CustomerStatus { get; set; } = "Lead";
        public decimal TotalRevenue { get; set; } = 0.00m;
        public string Source { get; set; } = "";
        public string Notes { get; set; } = "";

        // System Fields
        public bool IsActive { get; set; } = true;
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
        public int? ArchivedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Computed properties for display
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string SegmentName { get; set; } = "";
    }
}