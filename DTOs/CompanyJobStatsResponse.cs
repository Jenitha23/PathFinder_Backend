namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for company job statistics.
    /// </summary>
    public class CompanyJobStatsResponse
    {
        public int ActiveJobs { get; set; }
        public int ActiveInternships { get; set; }
        public int ActiveFullTimeJobs { get; set; }
        public int TotalApplicants { get; set; }
    }
}