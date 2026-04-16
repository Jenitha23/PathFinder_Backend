using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = "";
        
        [Required(ErrorMessage = "User type is required")]
        [RegularExpression("^(STUDENT|COMPANY)$", ErrorMessage = "User type must be STUDENT or COMPANY")]
        public string UserType { get; set; } = "";
    }
}