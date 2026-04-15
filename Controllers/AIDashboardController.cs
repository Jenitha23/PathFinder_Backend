using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AIDashboardController : ControllerBase
    {
        private readonly AiAnalyticsRepository _aiRepo;
        private readonly AtsScoringService _atsService;
        private readonly JobMatchingService _matchingService;
        private readonly ILogger<AIDashboardController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly CvTextExtractorService _cvExtractor;

        public AIDashboardController(
            AiAnalyticsRepository aiRepo,
            AtsScoringService atsService,
            JobMatchingService matchingService,
            ILogger<AIDashboardController> logger,
            IWebHostEnvironment env,
            CvTextExtractorService cvExtractor)
        {
            _aiRepo = aiRepo;
            _atsService = atsService;
            _matchingService = matchingService;
            _logger = logger;
            _env = env;
            _cvExtractor = cvExtractor;
        }

        [HttpPost("student/ats/analyze")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> AnalyzeMyCv([FromBody] AtsAnalysisRequest? request = null)
        {
            try
            {
                var studentId = GetCurrentUserId();
                if (studentId == null) 
                    return Unauthorized(new { message = "Invalid token" });

                var (cvUrl, skills, technicalSkills, education, university, degree) = 
                    await _aiRepo.GetStudentProfileForAnalysisAsync(studentId.Value);

                if (string.IsNullOrEmpty(cvUrl))
                    return BadRequest(new { message = "Please upload your CV first.", code = "no_cv" });

                string cvText;
                try
                {
                    cvText = await _cvExtractor.ExtractTextFromCvAsync(cvUrl);
                    if (string.IsNullOrWhiteSpace(cvText))
                        return BadRequest(new { message = "Could not extract text from CV. Please ensure it's a valid PDF or DOCX file.", code = "cv_parsing_error" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract text from CV for student {StudentId}", studentId);
                    return StatusCode(500, new { message = "Failed to process CV file. Please try uploading again.", code = "cv_processing_error" });
                }

                AtsScoreResponse result;

                if (request != null && request.JobId > 0)
                {
                    var job = await _aiRepo.GetJobByIdAsync(request.JobId);
                    if (job == null) 
                        return NotFound(new { message = "Job not found" });

                    result = await _atsService.AnalyzeCvAgainstJobAsync(
                        cvText, job.Title, job.Description, job.Requirements, studentId.Value);
                    result.JobId = request.JobId;
                }
                else
                {
                    result = await _atsService.AnalyzeCvStandaloneAsync(cvText, studentId.Value);
                }

                return Ok(new { message = "ATS analysis completed successfully", result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing CV");
                return StatusCode(503, new { message = "AI service temporarily unavailable", error = _env.IsDevelopment() ? ex.Message : null });
            }
        }

        [HttpGet("student/match/jobs")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetJobMatches([FromQuery] int? limit = null)
        {
            try
            {
                var studentId = GetCurrentUserId();
                if (studentId == null) 
                    return Unauthorized(new { message = "Invalid token" });

                var (cvUrl, skills, technicalSkills, education, university, degree) = 
                    await _aiRepo.GetStudentProfileForAnalysisAsync(studentId.Value);

                if (string.IsNullOrEmpty(cvUrl))
                    return BadRequest(new { message = "Please upload your CV first.", code = "no_cv" });

                string cvText;
                try
                {
                    cvText = await _cvExtractor.ExtractTextFromCvAsync(cvUrl);
                    if (string.IsNullOrWhiteSpace(cvText))
                        return BadRequest(new { message = "Could not extract text from CV. Please ensure it's a valid PDF or DOCX file.", code = "cv_parsing_error" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract text from CV for student {StudentId}", studentId);
                    return StatusCode(500, new { message = "Failed to process CV file. Please try uploading again.", code = "cv_processing_error" });
                }

                var allSkills = $"{skills ?? ""} {technicalSkills ?? ""}";
                var allEducation = $"{education ?? ""} {university ?? ""} {degree ?? ""}";

                var jobs = await _aiRepo.GetActiveJobsAsync();
                if (jobs.Count == 0)
                    return Ok(new BatchJobMatchesResponse { Matches = new(), TotalJobsAnalyzed = 0 });

                var jobsToAnalyze = limit.HasValue ? jobs.Take(limit.Value).ToList() : jobs;
                
                var matches = await ProcessJobsInParallel(cvText, allSkills, allEducation, studentId.Value, jobsToAnalyze);

                return Ok(new BatchJobMatchesResponse 
                { 
                    Matches = matches.OrderByDescending(m => m.MatchPercentage).ToList(), 
                    TotalJobsAnalyzed = matches.Count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job matches");
                return StatusCode(503, new { message = "AI service temporarily unavailable" });
            }
        }

        [HttpGet("student/match/job/{jobId}")]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetJobMatchForSpecificJob(int jobId)
        {
            try
            {
                var studentId = GetCurrentUserId();
                if (studentId == null) 
                    return Unauthorized(new { message = "Invalid token" });

                var job = await _aiRepo.GetJobByIdAsync(jobId);
                if (job == null) 
                    return NotFound(new { message = "Job not found" });

                var (cvUrl, skills, technicalSkills, education, university, degree) = 
                    await _aiRepo.GetStudentProfileForAnalysisAsync(studentId.Value);

                if (string.IsNullOrEmpty(cvUrl))
                    return BadRequest(new { message = "Please upload your CV first.", code = "no_cv" });

                string cvText;
                try
                {
                    cvText = await _cvExtractor.ExtractTextFromCvAsync(cvUrl);
                    if (string.IsNullOrWhiteSpace(cvText))
                        return BadRequest(new { message = "Could not extract text from CV. Please ensure it's a valid PDF or DOCX file.", code = "cv_parsing_error" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract text from CV for student {StudentId}", studentId);
                    return StatusCode(500, new { message = "Failed to process CV file. Please try uploading again.", code = "cv_processing_error" });
                }

                var allSkills = $"{skills ?? ""} {technicalSkills ?? ""}";
                var allEducation = $"{education ?? ""} {university ?? ""} {degree ?? ""}";

                var match = await _matchingService.CalculateMatchAsync(
                    cvText, allSkills, allEducation, studentId.Value,
                    job.Id, job.Title, job.Description, job.Requirements, job.CompanyName);

                return Ok(match);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job match");
                return StatusCode(503, new { message = "AI service temporarily unavailable" });
            }
        }

        [HttpGet("company/ranked-applicants/{jobId}")]
        [Authorize(Roles = "COMPANY")]
        public async Task<IActionResult> GetRankedApplicants(int jobId)
        {
            try
            {
                var companyId = GetCurrentCompanyId();
                if (companyId == null) 
                    return Unauthorized(new { message = "Invalid token" });

                var job = await _aiRepo.GetJobByIdAsync(jobId);
                if (job == null || job.CompanyId != companyId.Value)
                    return NotFound(new { message = "Job not found or access denied" });

                var applicants = await _aiRepo.GetApplicantsForJobAsync(jobId, companyId.Value);
                if (applicants.Count == 0)
                    return Ok(new RankedApplicantsResponse { JobId = jobId, JobTitle = job.Title, Applicants = new() });

                var rankedApplicants = new List<RankedApplicantResponse>();
                
                var semaphore = new SemaphoreSlim(3);
                var tasks = applicants.Select(async applicant =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string cvText;
                        if (!string.IsNullOrEmpty(applicant.CvUrl))
                        {
                            cvText = await _cvExtractor.ExtractTextFromCvAsync(applicant.CvUrl);
                        }
                        else
                        {
                            cvText = $"Student {applicant.StudentName} has not uploaded a CV.";
                        }

                        var match = await _matchingService.CalculateMatchAsync(
                            cvText, applicant.Skills ?? "", "", applicant.StudentId,
                            jobId, job.Title, job.Description, job.Requirements, job.CompanyName);

                        return new RankedApplicantResponse
                        {
                            ApplicationId = applicant.ApplicationId,
                            StudentId = applicant.StudentId,
                            StudentName = applicant.StudentName,
                            StudentEmail = applicant.StudentEmail,
                            Rank = 0,
                            AtsScore = match.MatchPercentage,
                            MatchScore = match.MatchPercentage,
                            Reasoning = match.Recommendation,
                            TopSkills = match.MatchedSkills.Take(5).ToList(),
                            MissingRequirements = match.MissingSkills.Take(5).ToList(),
                            CvUrl = applicant.CvUrl,
                            ApplicationStatus = applicant.Status,
                            AppliedDate = applicant.AppliedDate
                        };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);
                rankedApplicants = results.ToList();

                rankedApplicants = rankedApplicants.OrderByDescending(a => a.MatchScore)
                    .Select((a, index) => { a.Rank = index + 1; return a; }).ToList();

                var averageScore = rankedApplicants.Count > 0 ? (int)rankedApplicants.Average(a => a.MatchScore) : 0;

                return Ok(new RankedApplicantsResponse
                {
                    JobId = jobId,
                    JobTitle = job.Title,
                    Applicants = rankedApplicants,
                    TotalApplicants = rankedApplicants.Count,
                    AverageScore = averageScore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ranking applicants");
                return StatusCode(503, new { message = "AI service temporarily unavailable" });
            }
        }

        [HttpGet("admin/insights")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetAdminInsights()
        {
            try
            {
                var stats = await _aiRepo.GetPlatformStatsAsync();
                var skillDistribution = await _aiRepo.GetJobSkillDistributionFromDatabaseAsync();

                var topSkills = skillDistribution.Take(10)
                    .Select(kv => new SkillTrend 
                    { 
                        SkillName = kv.Key, 
                        JobPostingsCount = kv.Value,
                        StudentsWithSkill = 0,
                        GapCount = 0,
                        GrowthRate = 0
                    })
                    .ToList();

                var insights = new AdminAiInsightsResponse
                {
                    TalentDemand = new TalentDemandInsights
                    {
                        MostSoughtAfterRole = topSkills.FirstOrDefault()?.SkillName ?? "Unknown",
                        FastestGrowingCategory = "Software Development",
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
                            stats.StuckPendingCompanies > 0 ? $"Review {stats.StuckPendingCompanies} pending companies." : "All companies processed.",
                            stats.AverageApplicantsPerJob < 5 ? "Consider promoting jobs." : "Good applicant volume."
                        }
                    },
                    TopInDemandSkills = topSkills,
                    IndustryTrends = new(),
                    Predictions = new List<Prediction>
                    {
                        new Prediction
                        {
                            Metric = "Job Growth",
                            PredictionText = $"Job postings expected to increase by {Math.Min(25, stats.NewStudentsLast30Days / 10)}% next month.",
                            ConfidenceScore = 75,
                            Timeframe = "30 days"
                        }
                    }
                };

                return Ok(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin insights");
                return StatusCode(503, new { message = "Service temporarily unavailable" });
            }
        }

        #region Private Helper Methods

        private async Task<List<JobMatchResponse>> ProcessJobsInParallel(
            string cvText, 
            string allSkills, 
            string allEducation, 
            int studentId, 
            List<JobInfo> jobs)
        {
            var semaphore = new SemaphoreSlim(5);
            var tasks = jobs.Select(async job =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await _matchingService.CalculateMatchAsync(
                        cvText, allSkills, allEducation, studentId,
                        job.Id, job.Title, job.Description, job.Requirements, job.CompanyName);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var id) ? id : null;
        }

        private int? GetCurrentCompanyId()
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdStr, out var id) ? id : null;
        }

        #endregion
    }
}