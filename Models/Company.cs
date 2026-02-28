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

        // Stored as a secure hash using BCrypt (never store plain passwords).
        public string PasswordHash { get; set; } = "";

        // Account approval state controlled by admin.
        // Possible values:
        // PENDING_APPROVAL | APPROVED | REJECTED
        public string Status { get; set; } = "PENDING_APPROVAL";

        public DateTime CreatedAt { get; set; }
    }
}