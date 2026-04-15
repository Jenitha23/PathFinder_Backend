namespace PATHFINDER_BACKEND.DTOs
{
    public class AtsAnalysisRequest
    {
        public int JobId { get; set; }
        public bool ForceRefresh { get; set; } = false;
    }
}