using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    public class AdminAiInsightsResponse
    {
        public TalentDemandInsights TalentDemand { get; set; } = new();
        public PlatformHealthInsights PlatformHealth { get; set; } = new();
        public List<SkillTrend> TopInDemandSkills { get; set; } = new();
        public List<IndustryTrend> IndustryTrends { get; set; } = new();
        public List<Prediction> Predictions { get; set; } = new();
        public string AiGeneratedSummary { get; set; } = "";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class TalentDemandInsights
    {
        public string MostSoughtAfterRole { get; set; } = "";
        public string FastestGrowingCategory { get; set; } = "";
        public double AverageApplicantsPerJob { get; set; }
        public int TotalActiveJobs { get; set; }
        public int TotalActiveStudents { get; set; }
        public double StudentToJobRatio { get; set; }
    }

    public class PlatformHealthInsights
    {
        public double MonthOverMonthGrowth { get; set; }
        public double ApplicationSuccessRate { get; set; }
        public int CompaniesNeedingAttention { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public int HealthScore { get; set; }
        public string AlertLevel { get; set; } = "Good";
    }

    public class SkillTrend
    {
        public string SkillName { get; set; } = "";
        public int JobPostingsCount { get; set; }
        public int StudentsWithSkill { get; set; }
        public int GapCount { get; set; }
        public double GrowthRate { get; set; }
    }

    public class IndustryTrend
    {
        public string Industry { get; set; } = "";
        public string Trend { get; set; } = "";
        public string Insight { get; set; } = "";
    }

    public class Prediction
    {
        public string Metric { get; set; } = "";
        public string PredictionText { get; set; } = "";
        public double ConfidenceScore { get; set; }
        public string Timeframe { get; set; } = "";
    }
}