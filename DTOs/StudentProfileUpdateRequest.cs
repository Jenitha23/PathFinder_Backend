using Microsoft.AspNetCore.Http;

namespace PATHFINDER_BACKEND.DTOs
{
    // multipart/form-data request
    public class StudentProfileUpdateRequest
    {
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Experience { get; set; }

        // CV file upload (PDF/DOC/DOCX)
        public IFormFile? CvFile { get; set; }
    }
}