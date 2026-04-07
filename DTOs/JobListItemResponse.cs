namespace PATHFINDER_BACKEND.DTOs
{
    public class JobListItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Requirements { get; set; }      
        public string? Responsibilities { get; set; } 
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Salary { get; set; }
        public DateTime Deadline { get; set; }
    }
}