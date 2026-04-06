namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Filter request for company listing.
    /// </summary>
    public class CompanyListFilterRequest
    {
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}