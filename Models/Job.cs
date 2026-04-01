namespace PATHFINDER_BACKEND.Models
{
    /// <summary>
    /// Database entity representing a job posting.
    /// </summary>
    public class Job
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Requirements { get; set; } = "";
        public string? Responsibilities { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string CompanyEmail { get; set; } = "";
        public string Location { get; set; } = "";
        public string? Salary { get; set; }
        public string? SalaryRange { get; set; }
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string? ExperienceLevel { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsFeatured { get; set; }
        public int ViewsCount { get; set; }
        public DateTime PostedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}