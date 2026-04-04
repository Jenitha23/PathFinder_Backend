using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company/jobs/{jobId}/applications")]
    [Authorize(Roles = "COMPANY")]
    public class CompanyApplicationsController : ControllerBase
    {
        private readonly Db _db;
        private readonly ApplicationRepository _applicationRepo;
        private readonly CompanyRepository _companyRepo;

        public CompanyApplicationsController(Db db)
        {
            _db = db;
            _applicationRepo = new ApplicationRepository(_db);
            _companyRepo = new CompanyRepository(_db);
        }

        /// <summary>
        /// GET /api/company/jobs/{jobId}/applications
        /// Returns all applicants for a specific job posted by the authenticated company.
        /// Only the company that owns the job can access this endpoint.
        /// Supports optional status filtering.
        /// </summary>
        /// <param name="jobId">The job ID to get applicants for</param>
        /// <param name="status">Optional filter by application status (Pending, Shortlisted, Rejected, Accepted)</param>
        [HttpGet]
        public async Task<IActionResult> GetJobApplicants(int jobId, [FromQuery] string? status = null)
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Verify company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            // Ensure applications table exists
            await _applicationRepo.EnsureTableAndConstraintsAsync();

            // Validate status filter if provided
            if (!string.IsNullOrWhiteSpace(status))
            {
                var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
                if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = $"Invalid status filter '{status}'. Allowed values: {string.Join(", ", validStatuses)}.",
                        code = "invalid_status"
                    });
                }
            }

            // Get applicants with ownership validation (handled in repository)
            var applicants = await _applicationRepo.GetApplicantsByJobIdAsync(companyId, jobId, status?.Trim());

            if (applicants.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(status))
                {
                    return Ok(new
                    {
                        message = $"No applicants found with status '{status}' for this job.",
                        code = "no_applicants_for_status",
                        status = status.Trim(),
                        count = 0,
                        applicants = new List<ApplicantListResponse>()
                    });
                }

                return Ok(new
                {
                    message = "No applicants have applied for this job yet.",
                    code = "no_applicants",
                    count = 0,
                    applicants = new List<ApplicantListResponse>()
                });
            }

            return Ok(new
            {
                message = $"Found {applicants.Count} applicant(s).",
                count = applicants.Count,
                jobId = jobId,
                applicants
            });
        }

        /// <summary>
        /// GET /api/company/jobs/{jobId}/applications/{applicationId}
        /// Returns detailed information about a specific applicant including full profile.
        /// Only the company that owns the job can access this endpoint.
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <param name="applicationId">The application ID</param>
        [HttpGet("{applicationId:int}")]
        public async Task<IActionResult> GetApplicantDetails(int jobId, int applicationId)
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Verify company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            // Ensure applications table exists
            await _applicationRepo.EnsureTableAndConstraintsAsync();

            // Get applicant details with ownership validation
            var applicant = await _applicationRepo.GetApplicantDetailsAsync(companyId, jobId, applicationId);

            if (applicant == null)
            {
                return NotFound(new 
                { 
                    message = "Applicant not found or you don't have permission to view this application.",
                    code = "applicant_not_found"
                });
            }

            return Ok(new
            {
                message = "Applicant details retrieved successfully.",
                applicant
            });
        }

        /// <summary>
        /// PUT /api/company/jobs/{jobId}/applications/{applicationId}/status
        /// Updates the status of an application.
        /// Only the company that owns the job can update the status.
        /// Valid status values: Pending, Shortlisted, Rejected, Accepted.
        /// </summary>
        /// <param name="jobId">The job ID</param>
        /// <param name="applicationId">The application ID</param>
        /// <param name="request">The status update request</param>
        [HttpPut("{applicationId:int}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(
            int jobId, 
            int applicationId, 
            [FromBody] ApplicationStatusUpdateRequest request)
        {
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed.",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Verify company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            // Ensure applications table exists
            await _applicationRepo.EnsureTableAndConstraintsAsync();

            // Update the status (includes ownership validation)
            var updated = await _applicationRepo.UpdateApplicationStatusAsync(
                companyId, 
                jobId, 
                applicationId, 
                request.Status);

            if (!updated)
            {
                return NotFound(new 
                { 
                    message = "Application not found or you don't have permission to update this application.",
                    code = "update_failed"
                });
            }

            // Get the updated application details for response
            var updatedApplicant = await _applicationRepo.GetApplicantDetailsAsync(companyId, jobId, applicationId);

            return Ok(new
            {
                message = $"Application status updated to '{request.Status}' successfully.",
                applicationId = applicationId,
                jobId = jobId,
                newStatus = request.Status,
                updatedAt = DateTime.UtcNow,
                applicant = updatedApplicant != null ? new
                {
                    studentName = updatedApplicant.StudentName,
                    studentEmail = updatedApplicant.StudentEmail
                } : null
            });
        }

        /// <summary>
        /// GET /api/company/jobs/{jobId}/applications/stats
        /// Returns application statistics for a specific job.
        /// Includes counts for each status type.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetApplicationStats(int jobId)
        {
            // Get current company ID from token
            if (!TryGetCurrentCompanyId(out var companyId))
            {
                return Unauthorized(new { message = "Invalid token: missing userId." });
            }

            // Verify company exists
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound(new { message = "Company not found." });
            }

            // Verify company is approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new 
                { 
                    message = $"Company account is not approved. Current status: {company.Status}" 
                });
            }

            // First verify company owns this job
            var ownsJob = await _applicationRepo.VerifyCompanyOwnsJobAsync(companyId, jobId);
            if (!ownsJob)
            {
                return NotFound(new 
                { 
                    message = "Job not found or you don't have permission to view this job.",
                    code = "job_not_found"
                });
            }

            // Get all applicants for statistics
            var applicants = await _applicationRepo.GetApplicantsByJobIdAsync(companyId, jobId, null);

            var stats = new
            {
                total = applicants.Count,
                pending = applicants.Count(a => a.Status == "Pending"),
                shortlisted = applicants.Count(a => a.Status == "Shortlisted"),
                rejected = applicants.Count(a => a.Status == "Rejected"),
                accepted = applicants.Count(a => a.Status == "Accepted"),
                byDate = applicants
                    .GroupBy(a => a.AppliedDate.Date)
                    .Select(g => new { date = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.date)
                    .Take(7)
            };

            return Ok(new
            {
                message = "Application statistics retrieved successfully.",
                jobId = jobId,
                stats
            });
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