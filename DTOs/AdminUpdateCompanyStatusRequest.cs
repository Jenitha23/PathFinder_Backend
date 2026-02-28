using System.ComponentModel.DataAnnotations;

namespace PATHFINDER_BACKEND.DTOs
{
    /// <summary>
    /// Request payload to update company approval workflow status.
    /// Used by admin approval endpoint.
    /// </summary>
    public class AdminUpdateCompanyStatusRequest
    {
        [Required]
        public string Status { get; set; } = "";
    }
}