using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    public class JobMatchResponse
    {
        public int StudentId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int MatchPercentage { get; set; }
        public List<string> MatchedSkills { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<string> PartialMatches { get; set; } = new();
        public string Recommendation { get; set; } = "";
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BatchJobMatchesResponse
    {
        public List<JobMatchResponse> Matches { get; set; } = new();
        public int TotalJobsAnalyzed { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}