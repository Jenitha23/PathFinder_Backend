namespace PATHFINDER_BACKEND.Models
{
    /// <summary>
    /// Database entity for students.
    /// This represents the "students" table structure.
    /// </summary>
    public class Student
    {
        public int Id { get; set; }

        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        // Stored as a secure hash (never store plain passwords).
        public string PasswordHash { get; set; } = "";

        // Usually set by DB default (e.g., SYSUTCDATETIME()) and returned on select.
        public DateTime CreatedAt { get; set; }
    }
}