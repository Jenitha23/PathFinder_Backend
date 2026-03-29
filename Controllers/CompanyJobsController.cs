using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company/jobs")]
    [Authorize(Roles = "COMPANY")]
    public class CompanyJobsController : ControllerBase
    {
        private readonly CompanyJobRepository _jobRepo;
        private readonly CompanyRepository _companyRepo;

        public CompanyJobsController(CompanyJobRepository jobRepo, CompanyRepository companyRepo)
        {
            _jobRepo = jobRepo;
            _companyRepo = companyRepo;
        }

        /// <summary>
        /// POST /api/company/jobs
        /// Creates a new job posting for the authenticated company.
        /// Only approved companies can post jobs.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            // Manually validate required fields (to ensure we catch everything)
            var validationErrors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(request.Title))
                validationErrors.Add("Job title is required.");
                
            if (string.IsNullOrWhiteSpace(request.Description))
                validationErrors.Add("Job description is required.");
                
            if (string.IsNullOrWhiteSpace(request.Requirements))
                validationErrors.Add("Requirements are required.");
                
            if (string.IsNullOrWhiteSpace(request.Location))
                validationErrors.Add("Location is required.");
                
            if (string.IsNullOrWhiteSpace(request.JobType))
                validationErrors.Add("Job type is required.");
                
            if (string.IsNullOrWhiteSpace(request.Category))
                validationErrors.Add("Category is required.");
                
            if (request.ApplicationDeadline == default)
                validationErrors.Add("Application deadline is required.");
                
            if (validationErrors.Any())
            {
                return BadRequest(new 
                { 
                    message = "Validation failed.", 
                    errors = validationErrors 
                });
            }

            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new 
                { 
                    message = "Invalid token: missing userId." 
                });
            }

            // Verify company exists and is approved
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new 
                { 
                    message = "Company not found." 
                });
            }

            // Check if company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            // Validate deadline is in the future
            if (request.ApplicationDeadline.Date <= DateTime.UtcNow.Date)
            {
                return BadRequest(new 
                { 
                    message = "Job deadline must be a future date.", 
                    field = "applicationDeadline" 
                });
            }

            // Create the job
            try
            {
                var jobId = await _jobRepo.CreateJobAsync(companyId, request);
                if (jobId <= 0)
                {
                    return StatusCode(500, new 
                    { 
                        message = "Failed to create job posting. Please try again." 
                    });
                }

                // Return success response
                return Ok(new
                {
                    message = "Job posted successfully!",
                    jobId = jobId,
                    companyId = companyId,
                    companyName = company.CompanyName,
                    title = request.Title,
                    jobType = request.JobType,
                    category = request.Category,
                    location = request.Location,
                    deadline = request.ApplicationDeadline,
                    status = "Active"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = $"Error creating job: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// GET /api/company/jobs
        /// Returns all jobs posted by the authenticated company.
        /// Only approved companies can access this endpoint.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyJobs()
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists and is approved
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Check if company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            var jobs = await _jobRepo.GetJobsByCompanyIdAsync(companyId);

            if (jobs == null || jobs.Count == 0)
            {
                return Ok(new 
                { 
                    message = "You haven't posted any jobs yet.", 
                    count = 0,
                    jobs = new List<JobListItemResponse>() 
                });
            }

            return Ok(new
            {
                message = $"Found {jobs.Count} job(s).",
                count = jobs.Count,
                jobs
            });
        }

        /// <summary>
        /// GET /api/company/jobs/{jobId}
        /// Returns a specific job posted by the authenticated company.
        /// Companies can only view their own jobs.
        /// </summary>
        [HttpGet("{jobId:int}")]
        public async Task<IActionResult> GetMyJobById(int jobId)
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists and is approved
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Check if company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            var job = await _jobRepo.GetJobByCompanyAndIdAsync(companyId, jobId);
            if (job == null)
            {
                return NotFound(new { message = "Job not found or does not belong to your company." });
            }

            return Ok(job);
        }

        /// <summary>
        /// GET /api/company/jobs/stats
        /// Returns job statistics for the authenticated company.
        /// Includes active jobs, active internships, and total applicants.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetJobStats()
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists and is approved
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Check if company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            var stats = await _jobRepo.GetJobStatsAsync(companyId);
            return Ok(stats);
        }

        /// <summary>
        /// Helper method to extract company ID from JWT token.
        /// </summary>
        private bool TryGetCurrentCompanyId(out int companyId)
        {
            companyId = 0;

            var userId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out companyId);
        }
    }
}