namespace PATHFINDER_BACKEND.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Token { get; set; } = "";
        public string UserType { get; set; } = ""; // "STUDENT" or "COMPANY"
        public bool Used { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}