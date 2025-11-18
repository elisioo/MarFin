using System;

namespace MarFin_Final.Models
{
    public class SalesPipelineModel
    {
        // Primary Key
        public int OpportunityId { get; set; }

        // Foreign Keys
        public int CustomerId { get; set; }
        public int AssignedTo { get; set; }
        public int StageId { get; set; }

        // Opportunity Information
        public string OpportunityName { get; set; } = "";
        public decimal DealValue { get; set; } = 0.00m;
        public int Probability { get; set; } = 50;
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime? ActualCloseDate { get; set; }
        public string CloseReason { get; set; } = "";
        public string Notes { get; set; } = "";

        // System Fields
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Computed properties for display (from joins)
        public string CustomerName { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string StageName { get; set; } = "";
        public int StageOrder { get; set; }
        public string StageColor { get; set; } = "";
        public string AssignedToName { get; set; } = "";
        public decimal WeightedValue => DealValue * Probability / 100;
        public int DaysUntilClose => ExpectedCloseDate.HasValue
            ? (ExpectedCloseDate.Value - DateTime.Today).Days
            : 0;
    }

    public class PipelineStageModel
    {
        // Primary Key
        public int StageId { get; set; }

        // Stage Information
        public string StageName { get; set; } = "";
        public int StageOrder { get; set; }
        public int DefaultProbability { get; set; } = 50;
        public string Description { get; set; } = "";
        public bool IsClosed { get; set; } = false;
        public string StageColor { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }
}