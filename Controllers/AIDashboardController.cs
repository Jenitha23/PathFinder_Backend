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
        private readonly AiInsightsGeneratorService _aiInsightsGenerator;

        public AIDashboardController(
            AiAnalyticsRepository aiRepo,
            AtsScoringService atsService,
            JobMatchingService matchingService,
            ILogger<AIDashboardController> logger,
            IWebHostEnvironment env,
            CvTextExtractorService cvExtractor,
            AiInsightsGeneratorService aiInsightsGenerator)
        {
            _aiRepo = aiRepo;
            _atsService = atsService;
            _matchingService = matchingService;
            _logger = logger;
            _env = env;
            _cvExtractor = cvExtractor;
            _aiInsightsGenerator = aiInsightsGenerator;
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
        public async Task<IActionResult> GetAdminInsights([FromQuery] bool useAI = true)
        {
            try
            {
                var adminId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { message = "Invalid admin token" });
                }

                AdminAiInsightsResponse insights;
                
                if (useAI)
                {
                    insights = await _aiInsightsGenerator.GenerateAiInsightsAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "AI-powered admin insights retrieved successfully",
                        data = insights,
                        metadata = new
                        {
                            generatedBy = "Gemini AI",
                            generatedAt = DateTime.UtcNow,
                            version = "2.0",
                            aiEnabled = true
                        }
                    });
                }
                else
                {
                    insights = await GetBasicInsightsAsync();
                    return Ok(new
                    {
                        success = true,
                        message = "Basic admin insights retrieved (AI disabled)",
                        data = insights,
                        metadata = new
                        {
                            generatedBy = "Basic Analytics",
                            generatedAt = DateTime.UtcNow,
                            version = "2.0",
                            aiEnabled = false
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin insights");
                
                var fallbackInsights = await GetBasicInsightsAsync();
                return Ok(new
                {
                    success = false,
                    message = "AI service temporarily unavailable. Showing basic insights.",
                    data = fallbackInsights,
                    error = _env.IsDevelopment() ? ex.Message : null,
                    metadata = new
                    {
                        generatedBy = "Fallback Analytics",
                        generatedAt = DateTime.UtcNow,
                        version = "2.0",
                        aiEnabled = false,
                        errorOccurred = true
                    }
                });
            }
        }

        [HttpGet("admin/insights/health")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetPlatformHealth()
        {
            try
            {
                var stats = await _aiRepo.GetPlatformStatsAsync();
                var data = await _aiRepo.GetPlatformAnalyticsForAIAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "Platform health metrics retrieved",
                    data = new
                    {
                        stats.TotalStudents,
                        stats.ActiveJobs,
                        stats.AverageApplicantsPerJob,
                        stats.ApplicationSuccessRate,
                        stats.NewStudentsLast30Days,
                        stats.StuckPendingCompanies,
                        StudentEngagementRate = data.StudentEngagementRate,
                        HealthScore = CalculateHealthScore(stats, data),
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting platform health");
                return StatusCode(500, new { success = false, message = "Failed to retrieve platform health" });
            }
        }

        [HttpGet("admin/insights/skills-gap")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetSkillsGapAnalysis()
        {
            try
            {
                var jobSkills = await _aiRepo.GetJobSkillDistributionFromDatabaseAsync();
                var studentSkills = await _aiRepo.GetStudentSkillsDistributionAsync();
                
                var skillGaps = new List<SkillTrend>();
                
                foreach (var jobSkill in jobSkills.Take(20))
                {
                    var studentsWithSkill = studentSkills.GetValueOrDefault(jobSkill.Key, 0);
                    var gap = jobSkill.Value - studentsWithSkill;
                    
                    skillGaps.Add(new SkillTrend
                    {
                        SkillName = jobSkill.Key,
                        JobPostingsCount = jobSkill.Value,
                        StudentsWithSkill = studentsWithSkill,
                        GapCount = gap > 0 ? gap : 0,
                        GrowthRate = gap > 0 ? (gap * 100.0 / jobSkill.Value) : 0
                    });
                }
                
                return Ok(new
                {
                    success = true,
                    message = "Skills gap analysis completed",
                    data = new
                    {
                        totalSkillsAnalyzed = jobSkills.Count,
                        criticalGaps = skillGaps.Where(s => s.GapCount > s.JobPostingsCount / 2).Count(),
                        skillGaps = skillGaps.OrderByDescending(s => s.GapCount).ToList(),
                        generatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting skills gap analysis");
                return StatusCode(500, new { success = false, message = "Failed to retrieve skills gap analysis" });
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

        private async Task<AdminAiInsightsResponse> GetBasicInsightsAsync()
        {
            var stats = await _aiRepo.GetPlatformStatsAsync();
            var skills = await _aiRepo.GetJobSkillDistributionFromDatabaseAsync();
            var data = await _aiRepo.GetPlatformAnalyticsForAIAsync();
            
            return new AdminAiInsightsResponse
            {
                TalentDemand = new TalentDemandInsights
                {
                    MostSoughtAfterRole = skills.FirstOrDefault().Key ?? "Unknown",
                    FastestGrowingCategory = data.JobTrends.OrderByDescending(j => j.NewJobsLast3Months).FirstOrDefault()?.Category ?? "Technology",
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
                        stats.AverageApplicantsPerJob < 5 ? "Consider promoting jobs to increase applications" : "Good application volume",
                        data.StudentEngagementRate < 50 ? "Improve student engagement with personalized job alerts" : "Student engagement is healthy"
                    },
                    HealthScore = CalculateHealthScore(stats, data),
                    AlertLevel = stats.StuckPendingCompanies > 5 ? "Warning" : stats.StuckPendingCompanies > 0 ? "Attention" : "Good"
                },
                TopInDemandSkills = skills.Take(10).Select(s => new SkillTrend
                {
                    SkillName = s.Key,
                    JobPostingsCount = s.Value,
                    StudentsWithSkill = 0,
                    GapCount = s.Value,
                    GrowthRate = 10
                }).ToList(),
                IndustryTrends = data.JobTrends.Take(5).Select(j => new IndustryTrend
                {
                    Industry = j.Category,
                    Trend = j.NewJobsLast3Months > j.TotalJobs / 4 ? "Growing" : "Stable",
                    Insight = $"{j.Category} has {j.TotalJobs} positions, with {j.NewJobsLast3Months} new in last 3 months"
                }).ToList(),
                Predictions = new List<Prediction>
                {
                    new Prediction
                    {
                        Metric = "Job Growth",
                        PredictionText = $"Expected to grow by {Math.Min(20, stats.NewStudentsLast30Days / 5)}% in next quarter based on current trends",
                        ConfidenceScore = 70,
                        Timeframe = "90 days"
                    },
                    new Prediction
                    {
                        Metric = "Student Enrollment",
                        PredictionText = $"Expected {stats.NewStudentsLast30Days + 10} to {stats.NewStudentsLast30Days + 30} new students next month",
                        ConfidenceScore = 65,
                        Timeframe = "30 days"
                    }
                },
                AiGeneratedSummary = $"Platform has {stats.TotalStudents} students and {stats.ActiveJobs} active jobs. " +
                                      $"Student engagement is at {data.StudentEngagementRate:F1}% with {stats.ApplicationSuccessRate:F1}% success rate. " +
                                      $"Top in-demand skill is {skills.FirstOrDefault().Key} with {skills.FirstOrDefault().Value} job postings.",
                GeneratedAt = DateTime.UtcNow
            };
        }

        private int CalculateHealthScore(AiAnalyticsRepository.PlatformStats stats, Repositories.PlatformAnalyticsData data)
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
            
            if (stats.ActiveJobs > 100) score += 10;
            else if (stats.ActiveJobs > 50) score += 7;
            else if (stats.ActiveJobs > 20) score += 5;
            else if (stats.ActiveJobs > 10) score += 3;
            
            if (stats.AverageApplicantsPerJob > 20) score += 10;
            else if (stats.AverageApplicantsPerJob > 10) score += 7;
            else if (stats.AverageApplicantsPerJob > 5) score += 5;
            else if (stats.AverageApplicantsPerJob > 2) score += 3;
            
            var growthRate = stats.NewStudentsLast30Days > 0 ?
                (stats.NewStudentsLast30Days / (double)Math.Max(1, stats.TotalStudents - stats.NewStudentsLast30Days)) * 100 : 0;
            if (growthRate > 20) score += 10;
            else if (growthRate > 10) score += 7;
            else if (growthRate > 5) score += 5;
            else if (growthRate > 2) score += 3;
            
            return Math.Min(100, Math.Max(0, score));
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