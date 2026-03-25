namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for company profile data.
    /// Excludes sensitive fields like password hash.
    /// </summary>
    public class CompanyProfileResponse
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? LogoUrl { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}