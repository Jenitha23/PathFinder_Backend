using System;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for applications per job report.
    /// Admin can filter by optional jobId and date range.
    /// Company uses the same DTO but jobId must belong to them.
    /// </summary>
    public class ApplicationsPerJobReportRequest
    {
        /// <summary>
        /// Optional job ID filter. If null, returns all jobs (with company filtering).
        /// </summary>
        public int? JobId { get; set; }

        /// <summary>
        /// Start date for filtering applications (based on application applied_date)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for filtering applications (based on application applied_date)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Returns normalized date range. Defaults to last 30 days if none provided.
        /// </summary>
        public (DateTime? Start, DateTime? End) GetNormalizedDates()
        {
            DateTime? start = StartDate;
            DateTime? end = EndDate;

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
    }

    /// <summary>
    /// Response item for applications per job report.
    /// </summary>
    public class ApplicationsPerJobItem
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int TotalApplications { get; set; }
        public int PendingCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int RejectedCount { get; set; }
        public int AcceptedCount { get; set; }
        public DateTime JobPostedDate { get; set; }
    }

    /// <summary>
    /// Response envelope for applications per job report.
    /// </summary>
    public class ApplicationsPerJobReportResponse
    {
        public List<ApplicationsPerJobItem> Items { get; set; } = new();
        public int TotalJobs { get; set; }
        public int TotalApplications { get; set; }
        public bool IsEmpty { get; set; }
        public string? Message { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}