using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for updating application status by company.
    /// Company can change status to: Pending, Shortlisted, Rejected, Accepted.
    /// </summary>
    public class ApplicationStatusUpdateRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Pending|Shortlisted|Rejected|Accepted)$", 
            ErrorMessage = "Status must be one of: Pending, Shortlisted, Rejected, Accepted")]
        public string Status { get; set; } = "";
    }
}