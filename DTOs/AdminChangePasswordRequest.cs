using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for admin password change.
    /// Requires current password verification + new password policy.
    /// </summary>
    public class AdminChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        // Minimum length is a basic security requirement for new password.
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = "";
    }
}