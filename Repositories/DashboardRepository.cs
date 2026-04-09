using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PATHFINDER_BACKEND.Repositories
{
    /// <summary>
    /// Repository layer for dashboard analytics operations.
    /// Handles all SQL queries for statistics and chart data.
    /// </summary>
    public class DashboardRepository
    {
        private readonly Db _db;

        public DashboardRepository(Db db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets main dashboard statistics with optional date range filtering
        /// </summary>
        public async Task<DashboardStatsResponse> GetDashboardStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var result = new DashboardStatsResponse();

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Build date filter clause
            var dateFilter = "";
            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                dateFilter += " AND created_at >= @startDate";
                parameters.Add(new SqlParameter("@startDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                dateFilter += " AND created_at <= @endDate";
                parameters.Add(new SqlParameter("@endDate", endDate.Value.AddDays(1)));
            }

            // Total Students (active, not deleted)
            var studentSql = $@"
                SELECT COUNT(1) 
                FROM dbo.students 
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                {dateFilter.Replace("created_at", "created_at")}";

            using (var cmd = new SqlCommand(studentSql, conn))
            {
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalStudents = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total Companies (approved only for main count)
            var companySql = @"
                SELECT COUNT(1) 
                FROM dbo.companies 
                WHERE status = 'APPROVED'";

            using (var cmd = new SqlCommand(companySql, conn))
            {
                result.TotalCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total Jobs (active, not deleted)
            var jobSql = $@"
                SELECT COUNT(1) 
                FROM dbo.jobs 
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                {dateFilter.Replace("created_at", "created_at")}";

            using (var cmd = new SqlCommand(jobSql, conn))
            {
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalJobs = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Total Applications
            var appSql = $@"
                SELECT COUNT(1) 
                FROM dbo.applications a
                INNER JOIN dbo.jobs j ON a.job_id = j.id
                WHERE (j.is_deleted IS NULL OR j.is_deleted = 0)
                {dateFilter.Replace("created_at", "a.applied_date")}";

            using (var cmd = new SqlCommand(appSql, conn))
            {
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Additional Insights
            await GetInsightsAsync(conn, result.Insights);

            // Date range info
            result.DateRange.StartDate = startDate;
            result.DateRange.EndDate = endDate;
            result.DateRange.DisplayText = GetDateRangeDisplayText(startDate, endDate);

            return result;
        }

        /// <summary>
        /// Gets additional insights for the dashboard
        /// </summary>
        private async Task GetInsightsAsync(SqlConnection conn, DashboardInsights insights)
        {
            // Pending companies
            const string pendingCompaniesSql = "SELECT COUNT(1) FROM dbo.companies WHERE status = 'PENDING_APPROVAL'";
            using (var cmd = new SqlCommand(pendingCompaniesSql, conn))
            {
                insights.PendingCompanies = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // New students last 30 days
            const string newStudentsSql = @"
                SELECT COUNT(1) FROM dbo.students 
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND created_at >= DATEADD(DAY, -30, SYSUTCDATETIME())";
            using (var cmd = new SqlCommand(newStudentsSql, conn))
            {
                insights.NewStudentsLast30Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // New jobs last 30 days
            const string newJobsSql = @"
                SELECT COUNT(1) FROM dbo.jobs 
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND created_at >= DATEADD(DAY, -30, SYSUTCDATETIME())";
            using (var cmd = new SqlCommand(newJobsSql, conn))
            {
                insights.NewJobsLast30Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // New applications last 30 days
            const string newApplicationsSql = @"
                SELECT COUNT(1) FROM dbo.applications a
                INNER JOIN dbo.jobs j ON a.job_id = j.id
                WHERE (j.is_deleted IS NULL OR j.is_deleted = 0)
                    AND a.applied_date >= DATEADD(DAY, -30, SYSUTCDATETIME())";
            using (var cmd = new SqlCommand(newApplicationsSql, conn))
            {
                insights.NewApplicationsLast30Days = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Jobs expiring in next 7 days
            const string expiringJobsSql = @"
                SELECT COUNT(1) FROM dbo.jobs 
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND deadline >= CAST(SYSUTCDATETIME() AS DATE)
                    AND deadline <= DATEADD(DAY, 7, CAST(SYSUTCDATETIME() AS DATE))";
            using (var cmd = new SqlCommand(expiringJobsSql, conn))
            {
                insights.JobsExpiringSoon = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        /// <summary>
        /// Gets jobs posted per month data for chart (Direct SQL - No Views)
        /// </summary>
        public async Task<JobsPerMonthChart> GetJobsPerMonthDataAsync(DateTime? startDate, DateTime? endDate)
        {
            var chart = new JobsPerMonthChart();

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // Direct SQL query instead of using view
            var sql = @"
                SELECT 
                    YEAR(created_at) AS Year,
                    MONTH(created_at) AS Month,
                    DATENAME(MONTH, created_at) AS MonthName,
                    COUNT(*) AS JobCount,
                    COUNT(CASE WHEN type = 'Internship' THEN 1 END) AS InternshipCount,
                    COUNT(CASE WHEN type != 'Internship' OR type IS NULL THEN 1 END) AS FullTimeCount
                FROM dbo.jobs
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND created_at >= DATEADD(YEAR, -1, SYSUTCDATETIME())";

            var parameters = new List<SqlParameter>();

            if (startDate.HasValue)
            {
                sql += " AND created_at >= @startDate";
                parameters.Add(new SqlParameter("@startDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                sql += " AND created_at <= @endDate";
                parameters.Add(new SqlParameter("@endDate", endDate.Value.AddDays(1)));
            }

            sql += " GROUP BY YEAR(created_at), MONTH(created_at), DATENAME(MONTH, created_at)";
            sql += " ORDER BY Year ASC, Month ASC";

            var monthNames = new List<string>();
            var jobCounts = new List<int>();
            var internshipCounts = new List<int>();
            var fullTimeCounts = new List<int>();

            using (var cmd = new SqlCommand(sql, conn))
            {
                foreach (var p in parameters) cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    monthNames.Add($"{reader.GetInt32(0)}-{reader.GetString(2)}");
                    jobCounts.Add(reader.GetInt32(3));
                    internshipCounts.Add(reader.GetInt32(4));
                    fullTimeCounts.Add(reader.GetInt32(5));
                }
            }

            chart.Labels = monthNames;

            if (jobCounts.Count > 0)
            {
                chart.Datasets.Add(new JobsPerMonthDataPoint
                {
                    Label = "Total Jobs",
                    Data = jobCounts,
                    BorderColor = "#3B82F6",
                    BackgroundColor = "rgba(59, 130, 246, 0.1)"
                });

                chart.Datasets.Add(new JobsPerMonthDataPoint
                {
                    Label = "Internships",
                    Data = internshipCounts,
                    BorderColor = "#10B981",
                    BackgroundColor = "rgba(16, 185, 129, 0.1)"
                });

                chart.Datasets.Add(new JobsPerMonthDataPoint
                {
                    Label = "Full Time",
                    Data = fullTimeCounts,
                    BorderColor = "#F59E0B",
                    BackgroundColor = "rgba(245, 158, 11, 0.1)"
                });
            }

            return chart;
        }

        /// <summary>
        /// Gets applications per job data (top 10 by application count) - Direct SQL
        /// </summary>
        public async Task<ApplicationsPerJobChart> GetApplicationsPerJobDataAsync(DateTime? startDate, DateTime? endDate)
        {
            var chart = new ApplicationsPerJobChart();

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT TOP 10
                    j.id AS JobId,
                    j.title AS JobTitle,
                    c.company_name AS CompanyName,
                    COUNT(a.id) AS TotalApplications,
                    COUNT(CASE WHEN a.status = 'Pending' THEN 1 END) AS PendingCount,
                    COUNT(CASE WHEN a.status = 'Shortlisted' THEN 1 END) AS ShortlistedCount,
                    COUNT(CASE WHEN a.status = 'Rejected' THEN 1 END) AS RejectedCount,
                    COUNT(CASE WHEN a.status = 'Accepted' THEN 1 END) AS AcceptedCount
                FROM dbo.jobs j
                INNER JOIN dbo.companies c ON j.company_id = c.id
                LEFT JOIN dbo.applications a ON a.job_id = j.id
                WHERE (j.is_deleted IS NULL OR j.is_deleted = 0)
                    AND c.status = 'APPROVED'";

            if (startDate.HasValue)
            {
                sql += " AND j.created_at >= @startDate";
            }
            if (endDate.HasValue)
            {
                sql += " AND j.created_at <= @endDate";
            }

            sql += @" GROUP BY j.id, j.title, c.company_name
                     ORDER BY TotalApplications DESC";

            using (var cmd = new SqlCommand(sql, conn))
            {
                if (startDate.HasValue) cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue) cmd.Parameters.AddWithValue("@endDate", endDate.Value.AddDays(1));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var jobTitle = reader.GetString(reader.GetOrdinal("JobTitle"));
                    var shortTitle = jobTitle.Length > 25 ? jobTitle.Substring(0, 22) + "..." : jobTitle;

                    chart.Labels.Add(shortTitle);
                    chart.Data.Add(reader.GetInt32(reader.GetOrdinal("TotalApplications")));
                    chart.Details.Add(new JobApplicationDetail
                    {
                        JobId = reader.GetInt32(reader.GetOrdinal("JobId")),
                        JobTitle = jobTitle,
                        CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                        TotalApplications = reader.GetInt32(reader.GetOrdinal("TotalApplications")),
                        PendingCount = reader.GetInt32(reader.GetOrdinal("PendingCount")),
                        ShortlistedCount = reader.GetInt32(reader.GetOrdinal("ShortlistedCount")),
                        RejectedCount = reader.GetInt32(reader.GetOrdinal("RejectedCount")),
                        AcceptedCount = reader.GetInt32(reader.GetOrdinal("AcceptedCount"))
                    });
                }
            }

            return chart;
        }

        /// <summary>
        /// Gets application status distribution for pie chart - Direct SQL
        /// </summary>
        public async Task<ApplicationStatusDistribution> GetStatusDistributionAsync(DateTime? startDate, DateTime? endDate)
        {
            var distribution = new ApplicationStatusDistribution();

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    a.status,
                    COUNT(*) AS Count
                FROM dbo.applications a
                INNER JOIN dbo.jobs j ON a.job_id = j.id
                WHERE (j.is_deleted IS NULL OR j.is_deleted = 0)";

            if (startDate.HasValue)
            {
                sql += " AND a.applied_date >= @startDate";
            }
            if (endDate.HasValue)
            {
                sql += " AND a.applied_date <= @endDate";
            }

            sql += " GROUP BY a.status";

            var statusColors = new Dictionary<string, string>
            {
                ["Pending"] = "#F59E0B",
                ["Shortlisted"] = "#3B82F6",
                ["Rejected"] = "#EF4444",
                ["Accepted"] = "#10B981"
            };

            using (var cmd = new SqlCommand(sql, conn))
            {
                if (startDate.HasValue) cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                if (endDate.HasValue) cmd.Parameters.AddWithValue("@endDate", endDate.Value.AddDays(1));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var status = reader.GetString(0);
                    var count = reader.GetInt32(1);
                    distribution.Total += count;

                    distribution.Items.Add(new StatusDistributionItem
                    {
                        Status = status,
                        Count = count,
                        Color = statusColors.GetValueOrDefault(status, "#6B7280")
                    });
                }
            }

            // Calculate percentages
            if (distribution.Total > 0)
            {
                foreach (var item in distribution.Items)
                {
                    item.Percentage = Math.Round(item.Count * 100.0m / distribution.Total, 2);
                }
            }

            return distribution;
        }

        /// <summary>
        /// Checks if there's any data in the system
        /// </summary>
        public async Task<bool> HasAnyDataAsync()
        {
            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 
                    CASE 
                        WHEN EXISTS(SELECT 1 FROM dbo.students WHERE (is_deleted IS NULL OR is_deleted = 0)) THEN 1
                        WHEN EXISTS(SELECT 1 FROM dbo.companies) THEN 1
                        WHEN EXISTS(SELECT 1 FROM dbo.jobs WHERE (is_deleted IS NULL OR is_deleted = 0)) THEN 1
                        ELSE 0
                    END AS HasData";

            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToBoolean(result);
        }

        private string GetDateRangeDisplayText(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue && !endDate.HasValue)
                return "Last 30 days";

            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate.Value.Date == endDate.Value.Date)
                    return startDate.Value.ToString("MMMM d, yyyy");

                if (startDate.Value.Year == endDate.Value.Year && startDate.Value.Month == endDate.Value.Month)
                    return $"{startDate.Value:MMMM d} - {endDate.Value:d}";

                return $"{startDate.Value:MMMM d, yyyy} - {endDate.Value:MMMM d, yyyy}";
            }

            if (startDate.HasValue)
                return $"Since {startDate.Value:MMMM d, yyyy}";

            return $"Until {endDate:MMMM d, yyyy}";
        }

        // ========== NEW METHOD FOR JOBS PER MONTH REPORT (with year/date range filtering) ==========

        /// <summary>
        /// Gets jobs per month data for a specific company (or all if companyId = null) with filtering.
        /// Used by company and admin reports.
        /// </summary>
        /// <param name="companyId">If provided, filter jobs by this company. Null = all companies (admin).</param>
        /// <param name="year">Optional specific year.</param>
        /// <param name="startDate">Optional custom start date (overridden by year if provided).</param>
        /// <param name="endDate">Optional custom end date.</param>
        public async Task<JobsPerMonthChart> GetJobsPerMonthReportAsync(
            int? companyId,
            int? year,
            DateTime? startDate,
            DateTime? endDate)
        {
            var (start, end) = new JobsPerMonthReportRequest
            {
                Year = year,
                StartDate = startDate,
                EndDate = endDate
            }.GetNormalizedDates();

            var chart = new JobsPerMonthChart();
            var labels = new List<string>();
            var jobCounts = new List<int>();

            await using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    FORMAT(created_at, 'yyyy-MM') AS MonthKey,
                    FORMAT(created_at, 'MMM yyyy') AS MonthName,
                    COUNT(*) AS JobCount
                FROM dbo.jobs
                WHERE (is_deleted IS NULL OR is_deleted = 0)
                    AND created_at >= @startDate
                    AND created_at <= @endDate";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@startDate", start),
                new SqlParameter("@endDate", end)
            };

            if (companyId.HasValue && companyId.Value > 0)
            {
                sql += " AND company_id = @companyId";
                parameters.Add(new SqlParameter("@companyId", companyId.Value));
            }

            sql += @" GROUP BY FORMAT(created_at, 'yyyy-MM'), FORMAT(created_at, 'MMM yyyy')
                      ORDER BY MIN(created_at) ASC";

            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parameters)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                labels.Add(reader.GetString(reader.GetOrdinal("MonthName")));
                jobCounts.Add(reader.GetInt32(reader.GetOrdinal("JobCount")));
            }

            chart.Labels = labels;
            if (jobCounts.Count > 0)
            {
                chart.Datasets.Add(new JobsPerMonthDataPoint
                {
                    Label = "Jobs Posted",
                    Data = jobCounts,
                    BorderColor = "#3B82F6",
                    BackgroundColor = "rgba(59, 130, 246, 0.1)"
                });
            }

            return chart;
        }
    }
}