using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Registration payload for a student user.
    /// The password must be at least 8 characters (basic security requirement).
    /// </summary>
    public class StudentRegisterRequest
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        // Minimum length protects from trivial passwords.
        // Hashing happens server-side (PasswordService) before storing.
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = "";
    }
}