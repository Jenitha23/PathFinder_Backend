using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for updating company profile.
    /// Supports both text fields and optional logo file upload.
    /// </summary>
    public class CompanyProfileUpdateRequest
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(150, ErrorMessage = "Company name cannot exceed 150 characters.")]
        public string CompanyName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [StringLength(150, ErrorMessage = "Industry cannot exceed 150 characters.")]
        public string? Industry { get; set; }

        [Url(ErrorMessage = "Invalid website URL format. Include http:// or https://")]
        [StringLength(300, ErrorMessage = "Website URL cannot exceed 300 characters.")]
        public string? Website { get; set; }

        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
        public string? Location { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string? Phone { get; set; }

        // Optional logo file upload
        public IFormFile? LogoFile { get; set; }

        // Flag to remove existing logo
        public bool RemoveLogo { get; set; }
    }
}