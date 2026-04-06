using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    public class BulkCompanyStatusUpdateRequest
    {
        [Required]
        public List<int> CompanyIds { get; set; } = new();

        [Required]
        [RegularExpression("^(APPROVED|REJECTED)$", 
            ErrorMessage = "Bulk operations only support APPROVED or REJECTED")]
        public string Status { get; set; } = "";

        public string? DefaultRejectionReason { get; set; }

        public string? AdminNotes { get; set; }

        public bool SendEmailNotifications { get; set; } = true;
    }
}