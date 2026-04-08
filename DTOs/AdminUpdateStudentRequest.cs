using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for admin to update student account.
    /// </summary>
    public class AdminUpdateStudentRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
        [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(ACTIVE|SUSPENDED)$", 
            ErrorMessage = "Status must be ACTIVE or SUSPENDED")]
        public string Status { get; set; } = "ACTIVE";

        [MaxLength(500, ErrorMessage = "Suspension reason cannot exceed 500 characters")]
        public string? SuspensionReason { get; set; }
    }
}