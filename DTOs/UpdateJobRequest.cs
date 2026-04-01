using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for updating an existing job posting.
    /// Only accessible to the company that owns the job.
    /// </summary>
    public class UpdateJobRequest
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, ErrorMessage = "Job title cannot exceed 200 characters.")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Job description is required.")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Requirements are required.")]
        public string Requirements { get; set; } = "";

        public string? Responsibilities { get; set; }

        [StringLength(100, ErrorMessage = "Salary cannot exceed 100 characters.")]
        public string? Salary { get; set; }

        public string? SalaryRange { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(150, ErrorMessage = "Location cannot exceed 150 characters.")]
        public string Location { get; set; } = "";

        [Required(ErrorMessage = "Job type is required.")]
        [StringLength(50, ErrorMessage = "Job type cannot exceed 50 characters.")]
        public string JobType { get; set; } = "";

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
        public string Category { get; set; } = "";

        public string? ExperienceLevel { get; set; }

        [Required(ErrorMessage = "Application deadline is required.")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDeadline { get; set; }
    }
}