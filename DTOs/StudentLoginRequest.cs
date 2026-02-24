using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    public class StudentLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
