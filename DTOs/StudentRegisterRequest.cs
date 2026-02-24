using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    public class StudentRegisterRequest
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = "";
    }
}
