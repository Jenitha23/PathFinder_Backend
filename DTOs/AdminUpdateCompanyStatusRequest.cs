using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    public class AdminUpdateCompanyStatusRequest
    {
        [Required]
        public string Status { get; set; } = "";
    }
}
