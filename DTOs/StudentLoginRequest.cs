using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Login payload for a student user.
    /// DataAnnotations ensure automatic request validation via ModelState.
    /// </summary>
    public class StudentLoginRequest
    {
        // Email is required and must be a valid email format.
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        // Password is required (actual verification happens in PasswordService).
        [Required]
        public string Password { get; set; } = "";
    }
}