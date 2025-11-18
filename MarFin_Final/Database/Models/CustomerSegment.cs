using System;

namespace MarFin_Final.Models
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