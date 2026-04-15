using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    public class RankedApplicantResponse
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        public string? Headline { get; set; }
        public int Rank { get; set; }
        public int AtsScore { get; set; }
        public int MatchScore { get; set; }
        public string Reasoning { get; set; } = "";
        public List<string> TopSkills { get; set; } = new();
        public List<string> MissingRequirements { get; set; } = new();
        public string? CvUrl { get; set; }
        public string ApplicationStatus { get; set; } = "";
        public DateTime AppliedDate { get; set; }
    }

    public class RankedApplicantsResponse
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public List<RankedApplicantResponse> Applicants { get; set; } = new();
        public int TotalApplicants { get; set; }
        public int AverageScore { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}