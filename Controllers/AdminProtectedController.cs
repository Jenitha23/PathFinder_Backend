using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/admin")]
    // All endpoints in this controller are ADMIN-only.
    [Authorize(Roles = "ADMIN")]
    public class AdminProtectedController : ControllerBase
    {
        private readonly AdminRepository _adminRepo;
        private readonly StudentRepository _studentRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly PasswordService _pwd;

        // Restrict allowed statuses to enforce approval workflow correctness.
        private static readonly HashSet<string> AllowedCompanyStatuses =
        [
            "PENDING_APPROVAL",
            "APPROVED",
            "REJECTED"
        ];

        public AdminProtectedController(
            AdminRepository adminRepo,
            StudentRepository studentRepo,
            CompanyRepository companyRepo,
            PasswordService pwd)
        {
            _adminRepo = adminRepo;
            _studentRepo = studentRepo;
            _companyRepo = companyRepo;
            _pwd = pwd;
        }

        /// <summary>
        /// Returns current admin profile from DB using adminId claim.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            // AdminId is stored as "userId" claim in JWT.
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null) return NotFound("Admin not found.");

            return Ok(new
            {
                userId = admin.Id,
                fullName = admin.FullName,
                email = admin.Email,
                role = "ADMIN",
                createdAt = admin.CreatedAt
            });
        }

        /// <summary>
        /// Updates admin profile (full name + email).
        /// Enforces email uniqueness to prevent duplicates.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(AdminUpdateProfileRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var email = req.Email.Trim().ToLowerInvariant();

            // Ensure new email isn't already used by another admin.
            var existing = await _adminRepo.GetByEmailAsync(email);
            if (existing != null && existing.Id != adminId)
                return Conflict("Email already registered.");

            var updated = await _adminRepo.UpdateProfileAsync(adminId, req.FullName.Trim(), email);
            if (!updated) return NotFound("Admin not found.");

            return Ok(new
            {
                message = "Profile updated successfully.",
                fullName = req.FullName.Trim(),
                email
            });
        }

        /// <summary>
        /// Changes admin password after verifying the current password.
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null) return NotFound("Admin not found.");

            // Verify old password before allowing change.
            if (!_pwd.Verify(req.CurrentPassword, admin.PasswordHash))
                return BadRequest("Current password is incorrect.");

            // Hash new password before storing.
            var newHash = _pwd.Hash(req.NewPassword);

            var updated = await _adminRepo.UpdatePasswordHashAsync(adminId, newHash);
            if (!updated) return StatusCode(500, "Failed to update password.");

            return Ok(new { message = "Password changed successfully." });
        }

        /// <summary>
        /// Lists students for admin dashboard.
        /// Does not include password hash.
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _studentRepo.GetAllAsync();

            // Return only safe fields (no password hash).
            return Ok(students.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Email,
                s.CreatedAt
            }));
        }

        /// <summary>
        /// Lists companies for admin dashboard including approval status.
        /// </summary>
        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _companyRepo.GetAllAsync();
            return Ok(companies.Select(c => new
            {
                c.Id,
                c.CompanyName,
                c.Email,
                c.Status,
                c.CreatedAt
            }));
        }

        /// <summary>
        /// Updates company status (approval workflow).
        /// Allowed: PENDING_APPROVAL, APPROVED, REJECTED.
        /// </summary>
        [HttpPatch("companies/{companyId:int}/status")]
        public async Task<IActionResult> UpdateCompanyStatus(int companyId, AdminUpdateCompanyStatusRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Normalize status to uppercase for consistency.
            var status = req.Status.Trim().ToUpperInvariant();

            if (!AllowedCompanyStatuses.Contains(status))
                return BadRequest("Status must be one of: PENDING_APPROVAL, APPROVED, REJECTED.");

            var updated = await _companyRepo.UpdateStatusAsync(companyId, status);
            if (!updated) return NotFound("Company not found.");

            return Ok(new
            {
                message = "Company status updated successfully.",
                companyId,
                status
            });
        }

        /// <summary>
        /// Extracts current admin ID from JWT claims.
        /// Token creation includes "userId" claim for easy lookup.
        /// </summary>
        private bool TryGetCurrentAdminId(out int adminId)
        {
            adminId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value;
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out adminId);
        }
    }
}