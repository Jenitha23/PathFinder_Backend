using System;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for dashboard analytics with date range filtering.
    /// </summary>
    public class DashboardAnalyticsRequest
    {
        /// <summary>
        /// Start date for filtering analytics (inclusive)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for filtering analytics (inclusive)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Validate and normalize date range
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
}