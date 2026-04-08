namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Statistics response for admin dashboard.
    /// </summary>
    public class AdminUserStatsResponse
    {
        // Student Stats
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int SuspendedStudents { get; set; }
        public int DeletedStudents { get; set; }
        
        // Company Stats
        public int TotalCompanies { get; set; }
        public int PendingCompanies { get; set; }
        public int ApprovedCompanies { get; set; }
        public int RejectedCompanies { get; set; }
        public int SuspendedCompanies { get; set; }
        public int DeletedCompanies { get; set; }
        
        // Summary
        public DateTime GeneratedAt { get; set; }
    }
}