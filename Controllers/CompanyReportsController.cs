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

        public CompanyReportsController(DashboardRepository dashboardRepo, CompanyRepository companyRepo)
        {
            _dashboardRepo = dashboardRepo;
            _companyRepo = companyRepo;
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