namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Registration payload for company accounts.
    /// Companies require admin approval before login is allowed.
    /// </summary>
    public class CompanyRegisterRequest
    {
        // Legal or display name of the company.
        public string CompanyName { get; set; } = "";

        // Unique email used for authentication.
        public string Email { get; set; } = "";

        // Password will be hashed before storing in database.
        public string Password { get; set; } = "";
    }
}