namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for admin delete operations.
    /// </summary>
    public class AdminDeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int UserId { get; set; }
        public string UserType { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSoftDelete { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}