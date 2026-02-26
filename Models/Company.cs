namespace PATHFINDER_BACKEND.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";

        // PENDING_APPROVAL | APPROVED | REJECTED
        public string Status { get; set; } = "PENDING_APPROVAL";

        public DateTime CreatedAt { get; set; }
    }
}