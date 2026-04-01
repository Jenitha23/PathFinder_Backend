namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for job deletion operations.
    /// </summary>
    public class DeleteJobResponse
    {
        public int JobId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsSoftDelete { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}