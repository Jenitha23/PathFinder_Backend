using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    /// <summary>
    /// Admin Dashboard Analytics Controller
    /// Provides analytics data for admin dashboard including charts and statistics.
    /// Only accessible to users with ADMIN role.
    /// </summary>
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "ADMIN")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly DashboardRepository _dashboardRepo;
        private readonly AdminRepository _adminRepo;

        public AdminDashboardController(
            DashboardRepository dashboardRepo,
            AdminRepository adminRepo)
        {
            _dashboardRepo = dashboardRepo;
            _adminRepo = adminRepo;
        }

        /// <summary>
        /// GET /api/admin/dashboard/stats
        /// Returns main dashboard statistics (total students, companies, jobs, applications)
        /// </summary>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Verify admin exists and is valid
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                return Unauthorized(new { message = "Admin not found." });

            // Normalize dates
            var (normalizedStart, normalizedEnd) = NormalizeDateRange(startDate, endDate);

            var stats = await _dashboardRepo.GetDashboardStatsAsync(normalizedStart, normalizedEnd);

            // Check for empty state
            var hasData = await _dashboardRepo.HasAnyDataAsync();
            if (!hasData)
            {
                return Ok(new
                {
                    message = "No data available yet. Start by adding students, companies, and jobs.",
                    isEmpty = true,
                    stats
                });
            }

            return Ok(new
            {
                message = "Dashboard statistics retrieved successfully.",
                isEmpty = false,
                stats
            });
        }

        /// <summary>
        /// GET /api/admin/dashboard/analytics
        /// Returns complete dashboard analytics including all chart data.
        /// This is the main endpoint for the dashboard page.
        /// </summary>
        /// <param name="request">Filter request with date range</param>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] DashboardAnalyticsRequest request)
        {
            // Verify admin exists and is valid
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                return Unauthorized(new { message = "Admin not found." });

            // Get normalized date range
            var (startDate, endDate) = request.GetNormalizedDates();

            // Check if there's any data in the system
            var hasData = await _dashboardRepo.HasAnyDataAsync();

            if (!hasData)
            {
                return Ok(new DashboardAnalyticsResponse
                {
                    Stats = new DashboardStatsResponse(),
                    JobsPerMonth = new JobsPerMonthChart(),
                    ApplicationsPerJob = new ApplicationsPerJobChart(),
                    StatusDistribution = new ApplicationStatusDistribution(),
                    IsEmpty = true,
                    EmptyMessage = "No data available yet. Start by adding students, companies, and jobs to see analytics."
                });
            }

            // Fetch all analytics data in parallel for better performance
            var statsTask = _dashboardRepo.GetDashboardStatsAsync(startDate, endDate);
            var jobsPerMonthTask = _dashboardRepo.GetJobsPerMonthDataAsync(startDate, endDate);
            var applicationsPerJobTask = _dashboardRepo.GetApplicationsPerJobDataAsync(startDate, endDate);
            var statusDistributionTask = _dashboardRepo.GetStatusDistributionAsync(startDate, endDate);

            await Task.WhenAll(statsTask, jobsPerMonthTask, applicationsPerJobTask, statusDistributionTask);

            var response = new DashboardAnalyticsResponse
            {
                Stats = await statsTask,
                JobsPerMonth = await jobsPerMonthTask,
                ApplicationsPerJob = await applicationsPerJobTask,
                StatusDistribution = await statusDistributionTask,
                IsEmpty = false
            };

            // Add empty state for individual charts if they have no data
            if (response.JobsPerMonth.Datasets.Count == 0 || response.JobsPerMonth.Labels.Count == 0)
            {
                response.JobsPerMonth.Datasets = new List<JobsPerMonthDataPoint>();
                response.JobsPerMonth.Labels = new List<string>();
            }

            if (response.ApplicationsPerJob.Labels.Count == 0)
            {
                response.ApplicationsPerJob.Labels = new List<string>();
                response.ApplicationsPerJob.Data = new List<int>();
                response.ApplicationsPerJob.Details = new List<JobApplicationDetail>();
            }

            if (response.StatusDistribution.Items.Count == 0)
            {
                response.StatusDistribution.Items = new List<StatusDistributionItem>();
                response.StatusDistribution.Total = 0;
            }

            return Ok(response);
        }

        /// <summary>
        /// GET /api/admin/dashboard/jobs-per-month
        /// Returns only jobs per month chart data (for refresh or separate component)
        /// </summary>
        [HttpGet("jobs-per-month")]
        public async Task<IActionResult> GetJobsPerMonth([FromQuery] DashboardAnalyticsRequest request)
        {
            if (!TryGetCurrentAdminId(out _))
                return Unauthorized(new { message = "Invalid token claims." });

            var (startDate, endDate) = request.GetNormalizedDates();
            var data = await _dashboardRepo.GetJobsPerMonthDataAsync(startDate, endDate);

            if (data.Datasets.Count == 0 || data.Labels.Count == 0)
            {
                return Ok(new
                {
                    message = "No job posting data available for the selected period.",
                    isEmpty = true,
                    data
                });
            }

            return Ok(new
            {
                message = "Jobs per month data retrieved successfully.",
                isEmpty = false,
                data
            });
        }

        /// <summary>
        /// GET /api/admin/dashboard/top-jobs
        /// Returns top jobs by application count
        /// </summary>
        [HttpGet("top-jobs")]
        public async Task<IActionResult> GetTopJobs([FromQuery] DashboardAnalyticsRequest request, [FromQuery] int limit = 10)
        {
            if (!TryGetCurrentAdminId(out _))
                return Unauthorized(new { message = "Invalid token claims." });

            var (startDate, endDate) = request.GetNormalizedDates();
            var data = await _dashboardRepo.GetApplicationsPerJobDataAsync(startDate, endDate);

            // Apply limit (repository already returns top 10)
            var limitedDetails = data.Details.Take(limit).ToList();

            if (limitedDetails.Count == 0)
            {
                return Ok(new
                {
                    message = "No job application data available.",
                    isEmpty = true,
                    jobs = new List<JobApplicationDetail>()
                });
            }

            return Ok(new
            {
                message = $"Top {limitedDetails.Count} jobs retrieved successfully.",
                isEmpty = false,
                jobs = limitedDetails
            });
        }

        /// <summary>
        /// GET /api/admin/dashboard/status-distribution
        /// Returns application status distribution
        /// </summary>
        [HttpGet("status-distribution")]
        public async Task<IActionResult> GetStatusDistribution([FromQuery] DashboardAnalyticsRequest request)
        {
            if (!TryGetCurrentAdminId(out _))
                return Unauthorized(new { message = "Invalid token claims." });

            var (startDate, endDate) = request.GetNormalizedDates();
            var data = await _dashboardRepo.GetStatusDistributionAsync(startDate, endDate);

            if (data.Items.Count == 0)
            {
                return Ok(new
                {
                    message = "No application data available for status distribution.",
                    isEmpty = true,
                    distribution = data
                });
            }

            return Ok(new
            {
                message = "Status distribution retrieved successfully.",
                isEmpty = false,
                distribution = data
            });
        }

        /// <summary>
        /// GET /api/admin/dashboard/date-range-options
        /// Returns available date range options for the dashboard
        /// </summary>
        [HttpGet("date-range-options")]
        public IActionResult GetDateRangeOptions()
        {
            var options = new[]
            {
                new { value = "last7days", label = "Last 7 Days", days = 7 },
                new { value = "last30days", label = "Last 30 Days", days = 30 },
                new { value = "last90days", label = "Last 90 Days", days = 90 },
                new { value = "last12months", label = "Last 12 Months", days = 365 },
                new { value = "all", label = "All Time", days = 0 }
            };

            return Ok(new
            {
                message = "Date range options retrieved successfully.",
                options,
                defaultRange = "last30days"
            });
        }

        /// <summary>
        /// GET /api/admin/dashboard/jobs-per-month-report
        /// Admin version of jobs per month report with filtering by year or date range.
        /// </summary>
        [HttpGet("jobs-per-month-report")]
        public async Task<IActionResult> GetJobsPerMonthReport([FromQuery] JobsPerMonthReportRequest request)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null)
                return Unauthorized(new { message = "Admin not found." });

            // Admin sees all companies (companyId = null)
            var chart = await _dashboardRepo.GetJobsPerMonthReportAsync(
                companyId: null,
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

        #region Private Helper Methods

        private bool TryGetCurrentAdminId(out int adminId)
        {
            adminId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value;
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out adminId);
        }

        private (DateTime? Start, DateTime? End) NormalizeDateRange(DateTime? startDate, DateTime? endDate)
        {
            DateTime? start = startDate;
            DateTime? end = endDate;

            // If only start date provided, set end to today
            if (start.HasValue && !end.HasValue)
            {
                end = DateTime.UtcNow.Date;
            }

            // If only end date provided, set start to 30 days before end
            if (!start.HasValue && end.HasValue)
            {
                start = end.Value.AddDays(-30);
            }

            // If no dates provided, default to last 30 days
            if (!start.HasValue && !end.HasValue)
            {
                end = DateTime.UtcNow.Date;
                start = end.Value.AddDays(-30);
            }

            // Ensure start is not after end
            if (start.HasValue && end.HasValue && start > end)
            {
                start = end;
            }

            return (start, end);
        }

        #endregion
    }
}