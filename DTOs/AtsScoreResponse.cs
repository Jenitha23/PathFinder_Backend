using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    public class AtsScoreResponse
    {
        public int StudentId { get; set; }
        public int Score { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public List<string> PresentKeywords { get; set; } = new();
        public string FormattingFeedback { get; set; } = "";
        public string? Recommendation { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public bool IsFromCache { get; set; } = false;
        public int? JobId { get; set; }
    }
}