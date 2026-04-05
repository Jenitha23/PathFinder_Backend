using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Enhanced request payload for company approval workflow.
    /// </summary>
    public class AdminUpdateCompanyStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(PENDING_APPROVAL|APPROVED|REJECTED)$", 
            ErrorMessage = "Status must be PENDING_APPROVAL, APPROVED, or REJECTED")]
        public string Status { get; set; } = "";

        [MaxLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters")]
        public string? RejectionReason { get; set; }

        [MaxLength(1000, ErrorMessage = "Internal notes cannot exceed 1000 characters")]
        public string? AdminNotes { get; set; }

        public bool SendEmailNotification { get; set; } = true;
    }
}