using Microsoft.AspNetCore.Http;
using System;

namespace PATHFINDER_BACKEND.DTOs
{
    // multipart/form-data request (text fields + optional CV file)
    public class StudentProfileUpdateRequest
    {
        // Existing
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }

        // More useful fields for job finding
        public string? Headline { get; set; }          // e.g., "Java Backend Developer | Spring Boot"
        public string? AboutMe { get; set; }           // short bio / summary

        // Contact
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        // Education (structured)
        public string? University { get; set; }
        public string? Degree { get; set; }
        public string? AcademicYear { get; set; }      // e.g., "Year 3 - Semester 2"
        public string? Gpa { get; set; }               // keep as string to avoid format issues

        // Skills (better separation)
        public string? TechnicalSkills { get; set; }   // "Java, Spring Boot, React"
        public string? SoftSkills { get; set; }        // "Teamwork, Leadership"
        public string? Languages { get; set; }         // "English, Sinhala"

        // Career preferences
        public string? CareerInterests { get; set; }   // "Backend, DevOps, Cloud"
        public string? PreferredJobType { get; set; }  // "Internship", "Full-time"
        public string? WorkMode { get; set; }          // "Remote", "Onsite", "Hybrid"
        public DateTime? AvailableFrom { get; set; }   // optional

        // Links
        public string? GithubUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? PortfolioUrl { get; set; }

        // Projects / Experience extras
        public string? ProjectsSummary { get; set; }
        public string? InternshipExperience { get; set; }
        public string? Certifications { get; set; }

        // CV file upload (PDF/DOC/DOCX)
        public IFormFile? CvFile { get; set; }

        public bool RemoveCv { get; set; }
    }
}