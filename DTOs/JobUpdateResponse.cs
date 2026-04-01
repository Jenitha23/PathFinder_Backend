namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for job update operations.
    /// </summary>
    public class JobUpdateResponse
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string JobType { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime Deadline { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Message { get; set; } = "";
    }
}