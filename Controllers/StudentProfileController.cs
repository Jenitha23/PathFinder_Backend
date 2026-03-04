using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Data;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student/profile")]
    public class StudentProfileController : ControllerBase
    {
        private readonly Db _db;
        private readonly IWebHostEnvironment _env;

        public StudentProfileController(Db db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ✅ GET /api/student/profile
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

        // ✅ PUT /api/student/profile/update-v2 (multipart/form-data)
        [HttpPut("update-v2")]
        [Authorize(Roles = "STUDENT")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        public async Task<IActionResult> UpdateProfile([FromForm] StudentProfileUpdateRequest req)
        {
            var studentId = GetStudentIdFromToken();
            if (studentId == null) return Unauthorized("Invalid token: missing userId.");

            // Validate CV file if provided
            string? savedCvPath = null;
            if (req.CvFile != null && req.CvFile.Length > 0)
            {
                // ✅ file size check (10MB max)
                if (req.CvFile.Length > 10 * 1024 * 1024)
                    return BadRequest("CV file is too large. Max 10MB.");

                // ✅ file type check (PDF/DOC/DOCX)
                var allowedExt = new[] { ".pdf", ".doc", ".docx" };
                var ext = Path.GetExtension(req.CvFile.FileName).ToLowerInvariant();

                if (!allowedExt.Contains(ext))
                    return BadRequest("Invalid CV file type. Allowed: PDF, DOC, DOCX.");

                // Save file to: <ContentRoot>/Uploads/CVs/student-{id}/
                var baseDir = Path.Combine(_env.ContentRootPath, "Uploads", "CVs", $"student-{studentId.Value}");
                Directory.CreateDirectory(baseDir);

                var fileName = $"cv_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var fullPath = Path.Combine(baseDir, fileName);

                using (var stream = System.IO.File.Create(fullPath))
                {
                    await req.CvFile.CopyToAsync(stream);
                }

                // Store relative path in DB
                savedCvPath = Path.Combine("Uploads", "CVs", $"student-{studentId.Value}", fileName)
                    .Replace("\\", "/");
            }

            var repo = new StudentProfileRepository(_db);

            // ✅ IMPORTANT: use the new method (safe migration)
            await repo.EnsureTableAndColumnsAsync();

            // ✅ IMPORTANT: use the new upsert method (saves all fields)
            await repo.UpsertStudentProfileAsync(studentId.Value, req, savedCvPath);

            return Ok(new
            {
                message = "Profile updated successfully.",
                cvUrl = savedCvPath // can be null if not uploaded
            });
        }

        private int? GetStudentIdFromToken()
        {
            // JwtTokenService adds claim: "userId"
            var userIdStr = User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(userIdStr))
            {
                // fallback: JWT sub claim
                userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            }

            if (int.TryParse(userIdStr, out var id)) return id;
            return null;
        }
    }
}