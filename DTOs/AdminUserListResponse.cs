namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Response for user list endpoints with pagination.
    /// </summary>
    public class AdminUserListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<object> Users { get; set; } = new();
    }

    /// <summary>
    /// Student list item DTO
    /// </summary>
    public class StudentListItemDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "STUDENT";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public int ApplicationsCount { get; set; }
        public bool HasProfile { get; set; }
    }

    /// <summary>
    /// Company list item DTO
    /// </summary>
    public class CompanyListItemDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "COMPANY";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? SuspensionReason { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public int JobsCount { get; set; }
        public int ApplicationsCount { get; set; }
        public bool HasCompleteProfile { get; set; }
    }
}