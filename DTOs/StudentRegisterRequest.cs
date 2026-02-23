namespace PATHFINDER_BACKEND.DTOs
{
    public class StudentRegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}