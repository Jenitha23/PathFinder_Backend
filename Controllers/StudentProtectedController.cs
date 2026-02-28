using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student")]
    [Authorize(Roles = "STUDENT")]
    public class StudentProtectedController : ControllerBase
    {
        private readonly StudentRepository _repo;

        public StudentProtectedController(StudentRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Sample protected endpoint.
        /// Demonstrates role-based authorization using JWT role claim.
        /// </summary>
        [HttpGet("me")]
        public IActionResult Me()
        {
            // Prefer stable claim lookups (avoid string contains).
            var userId = User.FindFirst("userId")?.Value
                      ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                     ?? User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                message = "You are authorized as STUDENT",
                userId,
                email
            });
        }

        /// <summary>
        /// Updates current student profile (full name + email).
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateStudentProfileRequest req)
        {
            if (!TryGetCurrentStudentId(out var studentId)) return Unauthorized("Invalid token claims.");
            if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("FullName and Email are required.");

            var email = req.Email.Trim().ToLowerInvariant();
            try
            {
                _ = new MailAddress(email);
            }
            catch
            {
                return BadRequest("Invalid email format.");
            }

            var existing = await _repo.GetByEmailAsync(email);
            if (existing != null && existing.Id != studentId)
                return Conflict("Email already registered.");

            var updated = await _repo.UpdateProfileAsync(studentId, req.FullName.Trim(), email);
            if (!updated) return NotFound("Student not found.");

            return Ok(new
            {
                message = "Profile updated successfully.",
                fullName = req.FullName.Trim(),
                email
            });
        }

        /// <summary>
        /// Deletes current student account.
        /// </summary>
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!TryGetCurrentStudentId(out var studentId)) return Unauthorized("Invalid token claims.");

            var deleted = await _repo.DeleteByIdAsync(studentId);
            if (!deleted) return NotFound("Student not found.");

            return Ok(new { message = "Student account deleted successfully." });
        }

        private bool TryGetCurrentStudentId(out int studentId)
        {
            studentId = 0;

            var userId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out studentId);
        }

        public class UpdateStudentProfileRequest
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
        }
    }
}
