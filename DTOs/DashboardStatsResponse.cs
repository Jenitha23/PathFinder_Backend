using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for main dashboard statistics.
    /// </summary>
    public class DashboardStatsResponse
    {
        /// <summary>
        /// Total number of students (active, not deleted)
        /// </summary>
        public int TotalStudents { get; set; }

        /// <summary>
        /// Total number of approved companies
        /// </summary>
        public int TotalCompanies { get; set; }

        /// <summary>
        /// Total number of active jobs (not deleted, deadline not passed)
        /// </summary>
        public int TotalJobs { get; set; }

        /// <summary>
        /// Total number of job applications
        /// </summary>
        public int TotalApplications { get; set; }

        /// <summary>
        /// Additional insights
        /// </summary>
        public DashboardInsights Insights { get; set; } = new();

        /// <summary>
        /// Date range used for the statistics
        /// </summary>
        public DateRangeInfo DateRange { get; set; } = new();

        /// <summary>
        /// Timestamp when the data was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Additional insights for the dashboard
    /// </summary>
    public class DashboardInsights
    {
        /// <summary>
        /// Pending company approvals count
        /// </summary>
        public int PendingCompanies { get; set; }

        /// <summary>
        /// Students registered in the last 30 days
        /// </summary>
        public int NewStudentsLast30Days { get; set; }

        /// <summary>
        /// Jobs posted in the last 30 days
        /// </summary>
        public int NewJobsLast30Days { get; set; }

        /// <summary>
        /// Applications submitted in the last 30 days
        /// </summary>
        public int NewApplicationsLast30Days { get; set; }

        /// <summary>
        /// Jobs expiring in the next 7 days
        /// </summary>
        public int JobsExpiringSoon { get; set; }
    }

    /// <summary>
    /// Date range information for the dashboard
    /// </summary>
    public class DateRangeInfo
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DisplayText { get; set; } = "Last 30 days";
    }
}