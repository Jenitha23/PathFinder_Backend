using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for admin to update company account.
    /// </summary>
    public class AdminUpdateCompanyRequest
    {
        [Required(ErrorMessage = "Company name is required")]
        [MinLength(2, ErrorMessage = "Company name must be at least 2 characters")]
        [MaxLength(150, ErrorMessage = "Company name cannot exceed 150 characters")]
        public string CompanyName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(PENDING_APPROVAL|APPROVED|REJECTED|SUSPENDED)$", 
            ErrorMessage = "Status must be PENDING_APPROVAL, APPROVED, REJECTED, or SUSPENDED")]
        public string Status { get; set; } = "";

        [MaxLength(500, ErrorMessage = "Suspension reason cannot exceed 500 characters")]
        public string? SuspensionReason { get; set; }

        [MaxLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
        public string? AdminNotes { get; set; }
    }
}