using System.Text;
using System.Text.Json;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;

namespace PATHFINDER_BACKEND.Services
{
    public class AiInsightsGeneratorService
    {
        private readonly GeminiAIService _gemini;
        private readonly ILogger<AiInsightsGeneratorService> _logger;
        private readonly AiAnalyticsRepository _aiRepo;

        public AiInsightsGeneratorService(
            GeminiAIService gemini,
            ILogger<AiInsightsGeneratorService> logger,
            AiAnalyticsRepository aiRepo)
        {
            _gemini = gemini;
            _logger = logger;
            _aiRepo = aiRepo;
        }

        public async Task<AdminAiInsightsResponse> GenerateAiInsightsAsync()
        {
            try
            {
                var platformData = await _aiRepo.GetPlatformAnalyticsForAIAsync();
                var stats = await _aiRepo.GetPlatformStatsAsync();
                
                var aiAnalysis = await GetAiAnalysisAsync(platformData, stats);
                var skillGaps = CalculateSkillGaps(platformData.TopSkills, platformData.StudentSkills);
                
                // Convert string industry trends to IndustryTrend objects
                var industryTrends = aiAnalysis.IndustryTrends.Select(t => new IndustryTrend
                {
                    Industry = t.Length > 50 ? t.Substring(0, 50) : t,
                    Trend = "Observed",
                    Insight = t
                }).ToList();
                
                var insights = new AdminAiInsightsResponse
                {
                    TalentDemand = new TalentDemandInsights
                    {
                        MostSoughtAfterRole = aiAnalysis.MostSoughtAfterRole,
                        FastestGrowingCategory = aiAnalysis.FastestGrowingCategory,
                        AverageApplicantsPerJob = stats.AverageApplicantsPerJob,
                        TotalActiveJobs = stats.ActiveJobs,
                        TotalActiveStudents = stats.TotalStudents,
                        StudentToJobRatio = stats.ActiveJobs > 0 ? (double)stats.TotalStudents / stats.ActiveJobs : 0
                    },
                    PlatformHealth = new PlatformHealthInsights
                    {
                        MonthOverMonthGrowth = stats.NewStudentsLast30Days > 0 ?
                            (stats.NewStudentsLast30Days / (double)Math.Max(1, stats.TotalStudents - stats.NewStudentsLast30Days)) * 100 : 0,
                        ApplicationSuccessRate = stats.ApplicationSuccessRate,
                        CompaniesNeedingAttention = stats.StuckPendingCompanies,
                        Recommendations = aiAnalysis.Recommendations,
                        HealthScore = aiAnalysis.HealthScore,
                        AlertLevel = aiAnalysis.AlertLevel
                    },
                    TopInDemandSkills = skillGaps,
                    IndustryTrends = industryTrends,
                    Predictions = aiAnalysis.Predictions,
                    AiGeneratedSummary = aiAnalysis.Summary,
                    GeneratedAt = DateTime.UtcNow
                };
                
                return insights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI insights, falling back to basic insights");
                return await GetFallbackInsightsAsync();
            }
        }
        
        private async Task<AiAnalysisResult> GetAiAnalysisAsync(PlatformAnalyticsData data, AiAnalyticsRepository.PlatformStats stats)
        {
            var systemInstruction = "You are an expert data analyst and recruitment platform strategist. Analyze the platform data and provide strategic insights. Return ONLY valid JSON.";

            var jobTrendsJson = JsonSerializer.Serialize(data.JobTrends.Take(10));
            var studentTrendsJson = JsonSerializer.Serialize(data.StudentTrends);
            var appStatusJson = JsonSerializer.Serialize(data.ApplicationStatusDistribution);
            var topCompaniesJson = JsonSerializer.Serialize(data.TopCompanies);
            var topSkillsJson = JsonSerializer.Serialize(data.TopSkills.Take(10));
            var studentSkillsJson = JsonSerializer.Serialize(data.StudentSkills.Take(10));

            var prompt = $@"
Analyze this job platform data and provide strategic insights:

## PLATFORM OVERVIEW
- Total Students: {data.TotalStudents}
- Students with Applications: {data.StudentsWithApplications} ({data.StudentEngagementRate:F1}% engagement)
- Active Jobs: {stats.ActiveJobs}
- Total Applications: {stats.TotalApplications}
- Application Success Rate: {stats.ApplicationSuccessRate:F1}%

## JOB TRENDS BY CATEGORY
{jobTrendsJson}

## STUDENT GROWTH TRENDS (Last 6 months)
{studentTrendsJson}

## APPLICATION STATUS DISTRIBUTION
{appStatusJson}

## TOP 5 ACTIVE COMPANIES
{topCompaniesJson}

## TOP SKILLS IN DEMAND
{topSkillsJson}

## STUDENT SKILLS AVAILABLE
{studentSkillsJson}

## REQUIRED OUTPUT (JSON only) - IMPORTANT: industryTrends must be an array of strings, not objects:
{{
    ""mostSoughtAfterRole"": ""string - the most in-demand job title/role based on job postings"",
    ""fastestGrowingCategory"": ""string - fastest growing job category based on 3-month trend"",
    ""healthScore"": 0,
    ""alertLevel"": ""string - 'Critical', 'Warning', or 'Good'"",
    ""recommendations"": [ ""string - actionable recommendations"" ],
    ""industryTrends"": [ ""string - each trend as a simple string, not an object"" ],
    ""predictions"": [
        {{
            ""metric"": ""string - e.g., 'Job Growth', 'Student Enrollment'"",
            ""predictionText"": ""string - AI-generated prediction"",
            ""confidenceScore"": 0,
            ""timeframe"": ""string - e.g., '30 days', '90 days'""
        }}
    ],
    ""summary"": ""string - 2-3 sentence executive summary of platform health""
}}";

            try
            {
                var result = await _gemini.GenerateStructuredContentAsync<AiAnalysisResult>(prompt, systemInstruction);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analysis failed, using fallback");
                return GetFallbackAnalysisResult(data, stats);
            }
        }
        
        private List<SkillTrend> CalculateSkillGaps(Dictionary<string, int> jobSkills, Dictionary<string, int> studentSkills)
        {
            var skillGaps = new List<SkillTrend>();
            
            foreach (var jobSkill in jobSkills.Take(15))
            {
                var studentsWithSkill = studentSkills.GetValueOrDefault(jobSkill.Key, 0);
                var gap = jobSkill.Value - studentsWithSkill;
                
                skillGaps.Add(new SkillTrend
                {
                    SkillName = jobSkill.Key,
                    JobPostingsCount = jobSkill.Value,
                    StudentsWithSkill = studentsWithSkill,
                    GapCount = gap > 0 ? gap : 0,
                    GrowthRate = jobSkill.Value > 10 ? 15 : 5
                });
            }
            
            return skillGaps.OrderByDescending(s => s.GapCount).ToList();
        }
        
        private AiAnalysisResult GetFallbackAnalysisResult(PlatformAnalyticsData data, AiAnalyticsRepository.PlatformStats stats)
        {
            var firstCategory = data.JobTrends.FirstOrDefault()?.Category ?? "Software Developer";
            var growingCategory = data.JobTrends.OrderByDescending(j => j.NewJobsLast3Months).FirstOrDefault()?.Category ?? "Technology";
            
            return new AiAnalysisResult
            {
                MostSoughtAfterRole = firstCategory,
                FastestGrowingCategory = growingCategory,
                HealthScore = CalculateHealthScore(data, stats),
                AlertLevel = stats.StuckPendingCompanies > 5 ? "Warning" : "Good",
                Recommendations = new List<string>
                {
                    stats.AverageApplicantsPerJob < 3 ? "Consider marketing campaigns to attract more applicants" : "Good application volume",
                    data.StudentEngagementRate < 50 ? "Improve student engagement with personalized job alerts" : "Student engagement is healthy",
                    stats.StuckPendingCompanies > 0 ? $"Review {stats.StuckPendingCompanies} pending company registrations" : "All companies processed"
                },
                IndustryTrends = new List<string>(),
                Predictions = new List<Prediction>
                {
                    new Prediction
                    {
                        Metric = "Job Growth",
                        PredictionText = $"Expected to grow by {Math.Min(20, stats.NewStudentsLast30Days / 5)}% in next quarter",
                        ConfidenceScore = 75,
                        Timeframe = "90 days"
                    }
                },
                Summary = $"Platform has {stats.TotalStudents} students and {stats.ActiveJobs} active jobs. " +
                          $"Student engagement is at {data.StudentEngagementRate:F1}% with {stats.ApplicationSuccessRate:F1}% success rate."
            };
        }
        
        private int CalculateHealthScore(PlatformAnalyticsData data, AiAnalyticsRepository.PlatformStats stats)
        {
            int score = 50;
            
            if (data.StudentEngagementRate > 70) score += 20;
            else if (data.StudentEngagementRate > 50) score += 15;
            else if (data.StudentEngagementRate > 30) score += 10;
            else if (data.StudentEngagementRate > 10) score += 5;
            
            if (stats.ApplicationSuccessRate > 30) score += 20;
            else if (stats.ApplicationSuccessRate > 20) score += 15;
            else if (stats.ApplicationSuccessRate > 10) score += 10;
            else if (stats.ApplicationSuccessRate > 5) score += 5;
            
            if (stats.NewStudentsLast30Days > 100) score += 10;
            else if (stats.NewStudentsLast30Days > 50) score += 7;
            else if (stats.NewStudentsLast30Days > 20) score += 5;
            else if (stats.NewStudentsLast30Days > 10) score += 3;
            
            if (stats.StuckPendingCompanies == 0) score += 10;
            else if (stats.StuckPendingCompanies < 3) score += 5;
            
            var ratio = stats.ActiveJobs > 0 ? (double)stats.TotalStudents / stats.ActiveJobs : 0;
            if (ratio >= 3 && ratio <= 10) score += 10;
            else if (ratio >= 2 && ratio <= 15) score += 5;
            
            return Math.Min(100, Math.Max(0, score));
        }
        
        private async Task<AdminAiInsightsResponse> GetFallbackInsightsAsync()
        {
            var stats = await _aiRepo.GetPlatformStatsAsync();
            var skills = await _aiRepo.GetJobSkillDistributionFromDatabaseAsync();
            
            return new AdminAiInsightsResponse
            {
                TalentDemand = new TalentDemandInsights
                {
                    MostSoughtAfterRole = skills.FirstOrDefault().Key ?? "Unknown",
                    FastestGrowingCategory = "Technology",
                    AverageApplicantsPerJob = stats.AverageApplicantsPerJob,
                    TotalActiveJobs = stats.ActiveJobs,
                    TotalActiveStudents = stats.TotalStudents,
                    StudentToJobRatio = stats.ActiveJobs > 0 ? (double)stats.TotalStudents / stats.ActiveJobs : 0
                },
                PlatformHealth = new PlatformHealthInsights
                {
                    MonthOverMonthGrowth = stats.NewStudentsLast30Days > 0 ?
                        (stats.NewStudentsLast30Days / (double)Math.Max(1, stats.TotalStudents - stats.NewStudentsLast30Days)) * 100 : 0,
                    ApplicationSuccessRate = stats.ApplicationSuccessRate,
                    CompaniesNeedingAttention = stats.StuckPendingCompanies,
                    Recommendations = new List<string>
                    {
                        stats.StuckPendingCompanies > 0 ? $"Review {stats.StuckPendingCompanies} pending companies" : "No pending companies",
                        stats.AverageApplicantsPerJob < 5 ? "Consider promoting jobs to increase applications" : "Good application volume"
                    },
                    HealthScore = 70,
                    AlertLevel = stats.StuckPendingCompanies > 5 ? "Warning" : "Good"
                },
                TopInDemandSkills = skills.Take(10).Select(s => new SkillTrend
                {
                    SkillName = s.Key,
                    JobPostingsCount = s.Value,
                    StudentsWithSkill = 0,
                    GapCount = s.Value,
                    GrowthRate = 10
                }).ToList(),
                IndustryTrends = new List<IndustryTrend>(),
                Predictions = new List<Prediction>
                {
                    new Prediction
                    {
                        Metric = "Job Growth",
                        PredictionText = "Expected to grow steadily",
                        ConfidenceScore = 70,
                        Timeframe = "30 days"
                    }
                },
                AiGeneratedSummary = "AI insights temporarily unavailable. Showing basic analytics.",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public class AiAnalysisResult
    {
        public string MostSoughtAfterRole { get; set; } = "";
        public string FastestGrowingCategory { get; set; } = "";
        public int HealthScore { get; set; }
        public string AlertLevel { get; set; } = "";
        public List<string> Recommendations { get; set; } = new();
        public List<string> IndustryTrends { get; set; } = new();
        public List<Prediction> Predictions { get; set; } = new();
        public string Summary { get; set; } = "";
    }
}