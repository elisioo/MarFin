using System;

namespace MarFin_Final.Models
{
    public class Campaign
    {
        public int CampaignId { get; set; }
        public int CreatedBy { get; set; }
        public string CampaignName { get; set; } = "";
        public string CampaignType { get; set; } = "Email";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
        public decimal Budget { get; set; } = 0;
        public decimal ActualSpend { get; set; } = 0;
        public string? SubjectLine { get; set; }
        public string? EmailContent { get; set; }
        public string CampaignStatus { get; set; } = "Draft";
        public int TotalSent { get; set; } = 0;
        public int TotalOpened { get; set; } = 0;
        public int TotalClicked { get; set; } = 0;
        public int TotalConverted { get; set; } = 0;
        public decimal RevenueGenerated { get; set; } = 0;
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Computed properties
        public string CreatedByName { get; set; } = "";
        public decimal OpenRate => TotalSent > 0 ? Math.Round((decimal)TotalOpened / TotalSent * 100, 2) : 0;
        public decimal ClickRate => TotalSent > 0 ? Math.Round((decimal)TotalClicked / TotalSent * 100, 2) : 0;
        public decimal ConversionRate => TotalSent > 0 ? Math.Round((decimal)TotalConverted / TotalSent * 100, 2) : 0;
        public decimal ROI => ActualSpend > 0 ? Math.Round((RevenueGenerated - ActualSpend) / ActualSpend * 100, 2) : 0;
        public string DateRange => $"{StartDate:MMM dd} - {EndDate:MMM dd, yyyy}";
    }
}

