namespace PATHFINDER_BACKEND.Models
{
    /// <summary>
    /// Database entity representing an admin user.
    /// Corresponds to the "admins" table.
    /// </summary>
    public class Admin
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        // Stored hashed using BCrypt (never store plain passwords).
        public string PasswordHash { get; set; } = "";

        // Typically filled by DB default (e.g., SYSUTCDATETIME()).
        public DateTime CreatedAt { get; set; }
    }
}