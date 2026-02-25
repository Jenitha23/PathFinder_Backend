namespace PATHFINDER_BACKEND.DTOs
{
    public class CompanyRegisterRequest
    {
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}