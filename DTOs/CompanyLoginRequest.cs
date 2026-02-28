namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Login request payload for a company account.
    /// Used when a company attempts to authenticate.
    /// </summary>
    public class CompanyLoginRequest
    {
        // Email provided during registration.
        public string Email { get; set; } = "";

        // Plain password sent by client (validated & hashed server-side).
        public string Password { get; set; } = "";
    }
}