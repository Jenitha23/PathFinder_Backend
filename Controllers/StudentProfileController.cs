using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student/profile")]
    public class StudentProfileController : ControllerBase
    {
        private readonly Db _db;
        private readonly BlobService _blobService;

        public StudentProfileController(Db db, BlobService blobService)
        {
            _db = db;
            _blobService = blobService;
        }

        [HttpGet]
        [Authorize(Roles = "STUDENT")]
        public async Task<IActionResult> GetProfile()
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null) return Unauthorized("Invalid token: missing userId.");

            var repo = new StudentProfileRepository(_db);
            await repo.EnsureTableAndColumnsAsync();

            var profile = await repo.GetStudentProfileAsync(studentId.Value);
            if (profile == null) return NotFound("Student not found.");

            return Ok(profile);
        }

        [HttpPut("update-v2")]
        [Authorize(Roles = "STUDENT")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UpdateProfile([FromForm] StudentProfileUpdateRequest req)
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null) return Unauthorized("Invalid token: missing userId.");

            string? savedCvPath = null;

            if (req.CvFile != null && req.CvFile.Length > 0)
            {
                if (req.CvFile.Length > 10 * 1024 * 1024)
                    return BadRequest("CV file is too large. Max 10MB.");

                var allowedExt = new[] { ".pdf", ".doc", ".docx" };
                var ext = Path.GetExtension(req.CvFile.FileName).ToLowerInvariant();

                if (!allowedExt.Contains(ext))
                    return BadRequest("Invalid CV file type. Allowed: PDF, DOC, DOCX.");

                savedCvPath = await _blobService.UploadCvAsync(req.CvFile, studentId.Value);
            }

            var repo = new StudentProfileRepository(_db);
            await repo.EnsureTableAndColumnsAsync();
            await repo.UpsertStudentProfileAsync(studentId.Value, req, savedCvPath);

            return Ok(new
            {
                message = "Profile updated successfully.",
                cvUrl = savedCvPath
            });
        }

        private int? GetStudentIdFromToken()
        {
            var userIdStr = User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(userIdStr))
            {
                userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            }

            if (int.TryParse(userIdStr, out var id)) return id;
            return null;
        }
    }
}