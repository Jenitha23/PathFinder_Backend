namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response payload for a single applicant in the list view.
    /// Includes student profile information and application status.
    /// </summary>
    public class ApplicantListResponse
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string StudentEmail { get; set; } = "";
        public string? Headline { get; set; }
        public string? Skills { get; set; }
        public string? TechnicalSkills { get; set; }
        public string? Education { get; set; }
        public string? University { get; set; }
        public string? Degree { get; set; }
        public string? CvUrl { get; set; }
        public string Status { get; set; } = "";
        public string? CoverLetter { get; set; }
        public DateTime AppliedDate { get; set; }
    }
}