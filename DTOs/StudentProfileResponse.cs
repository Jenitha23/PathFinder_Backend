using System;

namespace PATHFINDER_BACKEND.DTOs
{
    public class StudentProfileResponse
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";

        // ✅ Basic Profile
        public string? Headline { get; set; }
        public string? AboutMe { get; set; }

        // ✅ Existing
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }

        // ✅ Contact
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        // ✅ Education (structured)
        public string? University { get; set; }
        public string? Degree { get; set; }
        public string? AcademicYear { get; set; }
        public string? Gpa { get; set; }

        // ✅ Skills categories
        public string? TechnicalSkills { get; set; }
        public string? SoftSkills { get; set; }
        public string? Languages { get; set; }

        // ✅ Career preferences
        public string? CareerInterests { get; set; }
        public string? PreferredJobType { get; set; }
        public string? WorkMode { get; set; }
        public DateTime? AvailableFrom { get; set; }

        // ✅ Links
        public string? GithubUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // ✅ Extra
        public string? ProjectsSummary { get; set; }
        public string? InternshipExperience { get; set; }
        public string? Certifications { get; set; }

        // ✅ CV
        public string? CvUrl { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}