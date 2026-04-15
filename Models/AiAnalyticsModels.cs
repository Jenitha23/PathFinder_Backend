using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.Models
{
    /// <summary>
    /// CV analysis result stored in database
    /// </summary>
    public class CvAnalysisResult
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? JobId { get; set; }
        public int AtsScore { get; set; }
        public int? MatchPercentage { get; set; }
        public string? Strengths { get; set; }
        public string? Suggestions { get; set; }
        public string? MissingKeywords { get; set; }
        public string? PresentKeywords { get; set; }
        public string? FormattingFeedback { get; set; }
        public string? Recommendation { get; set; }
        public string AnalysisType { get; set; } = "Standalone";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Job match analytics stored in database
    /// </summary>
    public class JobMatchAnalytics
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int StudentId { get; set; }
        public int MatchScore { get; set; }
        public string? MatchedSkills { get; set; }
        public string? MissingSkills { get; set; }
        public string? PartialMatches { get; set; }
        public string? Recommendation { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Applicant screening result stored in database
    /// </summary>
    public class ApplicantScreening
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public int ScreeningScore { get; set; }
        public string? ScreeningRecommendation { get; set; }
        public string? AiAnalysisJson { get; set; }
        public DateTime ScreenedAt { get; set; }
    }

    /// <summary>
    /// Analytics history snapshot for reporting
    /// </summary>
    public class AnalyticsHistory
    {
        public int Id { get; set; }
        public DateTime SnapshotDate { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveCompanies { get; set; }
        public int ActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public decimal? AvgAtsScore { get; set; }
        public decimal? AvgMatchPercentage { get; set; }
        public decimal? ApplicationSuccessRate { get; set; }
        public string? TopSkills { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}