namespace PATHFINDER_BACKEND.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public int? CompanyId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}