namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Enhanced response for company list items.
    /// </summary>
    public class CompanyListItemResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public int? TotalJobsPosted { get; set; }
        public int? TotalApplications { get; set; }
        public bool CanBeApproved { get; set; }
    }
}