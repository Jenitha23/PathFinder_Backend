namespace PATHFINDER_BACKEND.DTOs
{
    public class ApplicationResponse
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Location { get; set; } = "";
        public string JobType { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime AppliedDate { get; set; }
    }
}
