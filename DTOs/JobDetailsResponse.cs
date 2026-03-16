namespace PATHFINDER_BACKEND.DTOs
{
    public class JobDetailsResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string? Salary { get; set; }
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}