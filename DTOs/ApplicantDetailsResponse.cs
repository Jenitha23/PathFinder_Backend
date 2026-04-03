namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Detailed response payload for a single applicant.
    /// Includes full student profile information for detailed view.
    /// </summary>
    public class ApplicantDetailsResponse
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        
        // Profile Information
        public string? Headline { get; set; }
        public string? AboutMe { get; set; }
        public string? Skills { get; set; }
        public string? TechnicalSkills { get; set; }
        public string? SoftSkills { get; set; }
        public string? Languages { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }
        
        // Contact Information
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        
        // Education Details
        public string? University { get; set; }
        public string? Degree { get; set; }
        public string? AcademicYear { get; set; }
        public string? Gpa { get; set; }
        
        // Career Preferences
        public string? CareerInterests { get; set; }
        public string? PreferredJobType { get; set; }
        public string? WorkMode { get; set; }
        public DateTime? AvailableFrom { get; set; }
        
        // Links
        public string? GithubUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        
        // Projects & Certifications
        public string? ProjectsSummary { get; set; }
        public string? InternshipExperience { get; set; }
        public string? Certifications { get; set; }
        
        // Application Details
        public string? CoverLetter { get; set; }
        public string Status { get; set; } = "";
        public DateTime AppliedDate { get; set; }
        
        // CV
        public string? CvUrl { get; set; }
    }
}