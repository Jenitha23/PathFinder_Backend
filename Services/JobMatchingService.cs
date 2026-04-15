using Microsoft.Extensions.Logging;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.Repositories;

namespace PATHFINDER_BACKEND.Services
{
    public class JobMatchingService
    {
        private readonly GeminiAIService _gemini;
        private readonly AiAnalyticsRepository _aiRepo;
        private readonly ILogger<JobMatchingService> _logger;

        public JobMatchingService(GeminiAIService gemini, AiAnalyticsRepository aiRepo, ILogger<JobMatchingService> logger)
        {
            _gemini = gemini;
            _aiRepo = aiRepo;
            _logger = logger;
        }

        public async Task<JobMatchResponse> CalculateMatchAsync(
            string cvText, string studentSkills, string studentEducation, int studentId,
            int jobId, string jobTitle, string jobDescription, string jobRequirements, string companyName)
        {
            var systemInstruction = @"You are a job matching algorithm expert. Return ONLY valid JSON.";

            var prompt = $@"
Calculate the match percentage between this candidate and job.

CANDIDATE PROFILE:
CV Text: {cvText}
Skills: {studentSkills}
Education: {studentEducation}

JOB DETAILS:
Title: {jobTitle}
Description: {jobDescription}
Requirements: {jobRequirements}
Company: {companyName}

OUTPUT (JSON only):
{{
    ""matchPercentage"": number,
    ""matchedSkills"": [""string""],
    ""missingSkills"": [""string""],
    ""partialMatches"": [""string""],
    ""recommendation"": ""string""
}}";

            try
            {
                var result = await _gemini.GenerateStructuredContentAsync<JobMatchRaw>(prompt, systemInstruction);

                var response = new JobMatchResponse
                {
                    StudentId = studentId,
                    JobId = jobId,
                    JobTitle = jobTitle,
                    CompanyName = companyName,
                    MatchPercentage = result.MatchPercentage,
                    MatchedSkills = result.MatchedSkills ?? new(),
                    MissingSkills = result.MissingSkills ?? new(),
                    PartialMatches = result.PartialMatches ?? new(),
                    Recommendation = result.Recommendation ?? "",
                    CalculatedAt = DateTime.UtcNow
                };

                // Save to database
                await _aiRepo.SaveJobMatchAnalyticsAsync(new JobMatchAnalytics
                {
                    JobId = jobId,
                    StudentId = studentId,
                    MatchScore = response.MatchPercentage,
                    MatchedSkills = string.Join("|", response.MatchedSkills),
                    MissingSkills = string.Join("|", response.MissingSkills),
                    PartialMatches = string.Join("|", response.PartialMatches),
                    Recommendation = response.Recommendation
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate match for student {StudentId}, job {JobId}", studentId, jobId);
                throw;
            }
        }

        private class JobMatchRaw
        {
            public int MatchPercentage { get; set; }
            public List<string>? MatchedSkills { get; set; }
            public List<string>? MissingSkills { get; set; }
            public List<string>? PartialMatches { get; set; }
            public string? Recommendation { get; set; }
        }
    }
}