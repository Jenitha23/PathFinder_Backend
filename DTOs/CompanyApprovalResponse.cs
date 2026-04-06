namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for company approval operations.
    /// </summary>
    public class CompanyApprovalResponse
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string OldStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public string? RejectionReason { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string Message { get; set; } = "";
        public bool EmailSent { get; set; }
    }
}