using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company/reports")]
    [Authorize(Roles = "COMPANY")]
    public class CompanyReportsController : ControllerBase
    {
        private readonly DashboardRepository _dashboardRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly CompanyJobRepository _jobRepo;  // Added for job ownership validation

        public CompanyReportsController(
            DashboardRepository dashboardRepo, 
            CompanyRepository companyRepo,
            CompanyJobRepository jobRepo)  // Added dependency
        {
            _dashboardRepo = dashboardRepo;
            _companyRepo = companyRepo;
            _jobRepo = jobRepo;  // Store for validation
        }

        /// <summary>
        /// GET /api/company/reports/jobs-per-month
        /// Returns jobs posted per month for the authenticated company.
        /// Can filter by year or custom date range.
        /// </summary>
        [HttpGet("jobs-per-month")]
        public async Task<IActionResult> GetJobsPerMonth([FromQuery] JobsPerMonthReportRequest request)
        {
            if (!TryGetCurrentCompanyId(out var companyId))
                return Unauthorized(new { message = "Invalid token claims." });

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = $"Company account is not approved. Status: {company.Status}" });

            var chart = await _dashboardRepo.GetJobsPerMonthReportAsync(
                companyId: companyId,
                year: request.Year,
                startDate: request.StartDate,
                endDate: request.EndDate
            );

            if (chart.Labels == null || chart.Labels.Count == 0)
            {
                return Ok(new
                {
                    message = "No jobs posted in the selected period.",
                    isEmpty = true,
                    data = chart
                });
            }

            return Ok(new
            {
                message = "Jobs per month report retrieved successfully.",
                isEmpty = false,
                data = chart
            });
        }

        /// <summary>
        /// GET /api/company/reports/applications-per-job
        /// Returns applications per job report for the authenticated company.
        /// Can filter by optional jobId (must belong to company) and date range.
        /// </summary>
        [HttpGet("applications-per-job")]
        public async Task<IActionResult> GetApplicationsPerJobReport([FromQuery] ApplicationsPerJobReportRequest request)
        {
            if (!TryGetCurrentCompanyId(out var companyId))
                return Unauthorized(new { message = "Invalid token claims." });

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                return Unauthorized(new { message = $"Company account is not approved. Status: {company.Status}" });

            // If a specific jobId is provided, verify it belongs to this company
            if (request.JobId.HasValue)
            {
                var job = await _jobRepo.GetJobByCompanyAndIdAsync(companyId, request.JobId.Value);
                if (job == null)
                    return BadRequest(new { message = "Job not found or does not belong to your company." });
            }

            var (startDate, endDate) = request.GetNormalizedDates();

            var report = await _dashboardRepo.GetApplicationsPerJobReportAsync(
                companyId: companyId,
                jobId: request.JobId,
                startDate: startDate,
                endDate: endDate
            );

            if (report.IsEmpty)
            {
                return Ok(new
                {
                    message = "No applications found for the selected period.",
                    isEmpty = true,
                    report
                });
            }

            return Ok(new
            {
                message = "Applications per job report retrieved successfully.",
                isEmpty = false,
                report
            });
        }

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