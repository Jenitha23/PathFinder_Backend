using System.Text;
using System.Text.Json;
using PATHFINDER_BACKEND.DTOs;
using Microsoft.Extensions.Logging;

namespace PATHFINDER_BACKEND.Services
{
    /// <summary>
    /// Service for ATS score calculation using Gemini AI
    /// </summary>
    public class AtsScoringService
    {
        private readonly GeminiAIService _gemini;
        private readonly ILogger<AtsScoringService> _logger;

        public AtsScoringService(GeminiAIService gemini, ILogger<AtsScoringService> logger)
        {
            _gemini = gemini;
            _logger = logger;
        }

        /// <summary>
        /// Analyze CV against a specific job description
        /// </summary>
        public async Task<AtsScoreResponse> AnalyzeCvAgainstJobAsync(
            string cvText, 
            string jobTitle, 
            string jobDescription, 
            string jobRequirements,
            int studentId)
        {
            var systemInstruction = @"You are an expert ATS (Applicant Tracking System) and recruitment specialist. 
Analyze CVs against job requirements objectively. Return ONLY valid JSON without any markdown formatting.";

            var prompt = $@"
Analyze this CV against the job posting and provide an ATS score (0-100).

## JOB DETAILS
Title: {jobTitle}
Description: {jobDescription}
Requirements: {jobRequirements}

## CV TEXT
{cvText}

## REQUIRED OUTPUT (JSON only)
{{
    ""score"": number (0-100),
    ""strengths"": [string array of top 3-5 strengths],
    ""suggestions"": [string array of 3-5 actionable improvements],
    ""missingKeywords"": [string array of critical missing keywords],
    ""presentKeywords"": [string array of matched keywords],
    ""formattingFeedback"": ""string with formatting advice""
}}

Scoring rubric:
- 90-100: Excellent match, highly recommended
- 70-89: Good match, minor gaps
- 50-69: Partial match, significant gaps
- Below 50: Poor match, needs major improvements

Consider: keyword density, skills alignment, experience relevance, education fit.
";

            try
            {
                var result = await _gemini.GenerateStructuredContentAsync<AtsScoreRaw>(prompt, systemInstruction);
                
                return new AtsScoreResponse
                {
                    StudentId = studentId,
                    Score = result.Score,
                    Strengths = result.Strengths ?? new List<string>(),
                    Suggestions = result.Suggestions ?? new List<string>(),
                    MissingKeywords = result.MissingKeywords ?? new List<string>(),
                    PresentKeywords = result.PresentKeywords ?? new List<string>(),
                    FormattingFeedback = result.FormattingFeedback ?? "",
                    AnalyzedAt = DateTime.UtcNow,
                    IsFromCache = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze CV for student {StudentId}", studentId);
                throw;
            }
        }

        /// <summary>
        /// Analyze CV standalone (no job context)
        /// </summary>
        public async Task<AtsScoreResponse> AnalyzeCvStandaloneAsync(string cvText, int studentId)
        {
            var systemInstruction = @"You are an expert resume writer and ATS consultant.
Analyze the CV for quality and ATS compatibility. Return ONLY valid JSON.";

            var prompt = $@"
Analyze this CV for ATS compatibility and overall quality.

## CV TEXT
{cvText}

## REQUIRED OUTPUT (JSON only)
{{
    ""score"": number (0-100),
    ""strengths"": [string array of 3-5 strengths],
    ""suggestions"": [string array of 3-5 improvements],
    ""missingKeywords"": [string array of missing critical sections or keywords],
    ""presentKeywords"": [string array of good keywords found],
    ""formattingFeedback"": ""string with formatting advice""
}}

Evaluate based on:
- Use of standard section headers
- Keyword density
- Quantifiable achievements
- Contact information completeness
- File format compatibility
";

            try
            {
                var result = await _gemini.GenerateStructuredContentAsync<AtsScoreRaw>(prompt, systemInstruction);
                
                return new AtsScoreResponse
                {
                    StudentId = studentId,
                    Score = result.Score,
                    Strengths = result.Strengths ?? new List<string>(),
                    Suggestions = result.Suggestions ?? new List<string>(),
                    MissingKeywords = result.MissingKeywords ?? new List<string>(),
                    PresentKeywords = result.PresentKeywords ?? new List<string>(),
                    FormattingFeedback = result.FormattingFeedback ?? "",
                    AnalyzedAt = DateTime.UtcNow,
                    IsFromCache = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze CV standalone for student {StudentId}", studentId);
                throw;
            }
        }

        // Internal class for JSON deserialization
        private class AtsScoreRaw
        {
            public int Score { get; set; }
            public List<string>? Strengths { get; set; }
            public List<string>? Suggestions { get; set; }
            public List<string>? MissingKeywords { get; set; }
            public List<string>? PresentKeywords { get; set; }
            public string? FormattingFeedback { get; set; }
        }
    }
}