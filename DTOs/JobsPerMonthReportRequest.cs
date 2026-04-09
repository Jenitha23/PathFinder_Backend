using System;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload for jobs per month report.
    /// User can filter by year OR custom date range.
    /// </summary>
    public class JobsPerMonthReportRequest
    {
        /// <summary>
        /// Filter by specific year (e.g., 2025). If provided, StartDate/EndDate are ignored.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Start date for custom range (inclusive)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for custom range (inclusive)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Returns normalized date range. If Year is provided, returns Jan 1 - Dec 31 of that year.
        /// If custom dates provided, uses them. Defaults to last 12 months.
        /// </summary>
        public (DateTime Start, DateTime End) GetNormalizedDates()
        {
            if (Year.HasValue && Year.Value > 2000)
            {
                var start = new DateTime(Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var end = new DateTime(Year.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
                return (start, end);
            }

            if (StartDate.HasValue && EndDate.HasValue)
            {
                var start = StartDate.Value.Date;
                var end = EndDate.Value.Date.AddDays(1).AddSeconds(-1);
                return (start, end);
            }

            if (StartDate.HasValue)
            {
                var start = StartDate.Value.Date;
                var end = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
                return (start, end);
            }

            if (EndDate.HasValue)
            {
                var start = EndDate.Value.Date.AddYears(-1);
                var end = EndDate.Value.Date.AddDays(1).AddSeconds(-1);
                return (start, end);
            }

            // Default: last 12 months
            var defaultEnd = DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
            var defaultStart = defaultEnd.AddYears(-1);
            return (defaultStart, defaultEnd);
        }
    }
}