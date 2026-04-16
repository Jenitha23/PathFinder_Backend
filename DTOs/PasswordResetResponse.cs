namespace PATHFINDER_BACKEND.DTOs
{
    public class PasswordResetResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? ResetToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}