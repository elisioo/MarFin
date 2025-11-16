using System;

namespace Ordering_Sorono_IT13.Models
{
    public class CustomerSegment
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal MinRevenue { get; set; } = 0;
        public decimal? MaxRevenue { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }
}