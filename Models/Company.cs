namespace PATHFINDER_BACKEND.Models
{
    /// <summary>
    /// Represents the "companies" table in the database.
    /// Includes approval workflow support.
    /// </summary>
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Status { get; set; } = "PENDING_APPROVAL";
        public DateTime CreatedAt { get; set; }

        // Profile fields
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public string? LogoUrl { get; set; }

        // NEW: Approval workflow fields
        public string? RejectionReason { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}