using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload to update admin profile details.
    /// Email uniqueness is validated in controller/repository.
    /// </summary>
    public class AdminUpdateProfileRequest
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}