using System;
using System.Collections.Generic;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Complete response payload for dashboard analytics including charts data.
    /// </summary>
    public class DashboardAnalyticsResponse
    {
        /// <summary>
        /// Main statistics summary
        /// </summary>
        public DashboardStatsResponse Stats { get; set; } = new();

        /// <summary>
        /// Jobs posted per month data for chart
        /// </summary>
        public JobsPerMonthChart JobsPerMonth { get; set; } = new();

        /// <summary>
        /// Applications per job data for chart (top 10)
        /// </summary>
        public ApplicationsPerJobChart ApplicationsPerJob { get; set; } = new();

        /// <summary>
        /// Application status distribution for pie chart
        /// </summary>
        public ApplicationStatusDistribution StatusDistribution { get; set; } = new();

        /// <summary>
        /// Indicates if there's no data available
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Optional message for empty state
        /// </summary>
        public string? EmptyMessage { get; set; }
    }

    /// <summary>
    /// Jobs per month chart data
    /// </summary>
    public class JobsPerMonthChart
    {
        public List<string> Labels { get; set; } = new();
        public List<JobsPerMonthDataPoint> Datasets { get; set; } = new();
    }

    /// <summary>
    /// Individual data point for jobs per month
    /// </summary>
    public class JobsPerMonthDataPoint
    {
        public string Label { get; set; } = "";
        public List<int> Data { get; set; } = new();
        public string BorderColor { get; set; } = "";
        public string BackgroundColor { get; set; } = "";
    }

    /// <summary>
    /// Applications per job chart data (bar chart)
    /// </summary>
    public class ApplicationsPerJobChart
    {
        public List<string> Labels { get; set; } = new();  // Job titles
        public List<int> Data { get; set; } = new();       // Application counts
        public List<JobApplicationDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// Detailed information for each job in applications per job chart
    /// </summary>
    public class JobApplicationDetail
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int RejectedCount { get; set; }
        public int AcceptedCount { get; set; }
    }

    /// <summary>
    /// Application status distribution for pie chart
    /// </summary>
    public class ApplicationStatusDistribution
    {
        public List<StatusDistributionItem> Items { get; set; } = new();
        public int Total { get; set; }
    }

    /// <summary>
    /// Individual status distribution item
    /// </summary>
    public class StatusDistributionItem
    {
        public string Status { get; set; } = "";
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = "";
    }
}