using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Login request payload for Admin authentication.
    /// DataAnnotations enforce validation before controller logic runs.
    /// </summary>
    public class AdminLoginRequest
    {
        // Email is required and must match a valid email format.
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        // Password is required. Verification happens against stored hash.
        [Required]
        public string Password { get; set; } = "";
    }
}