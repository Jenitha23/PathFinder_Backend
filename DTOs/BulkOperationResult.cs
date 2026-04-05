namespace PATHFINDER_BACKEND.DTOs
{
    public class BulkOperationResult
    {
        public int CompanyId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}