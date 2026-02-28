namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Standard response payload for authentication endpoints.
    /// Keeps response consistent across Student/Company/Admin.
    /// </summary>
    public class AuthResponse
    {
        public string Token { get; set; } = "";
        public int UserId { get; set; }
        public string Role { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
    }
}