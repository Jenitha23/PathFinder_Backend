namespace PATHFINDER_BACKEND.DTOs
{
    public class StudentProfileResponse
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";

        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }

        public string? CvUrl { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}