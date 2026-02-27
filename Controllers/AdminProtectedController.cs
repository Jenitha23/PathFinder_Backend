using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminProtectedController : ControllerBase
    {
        private readonly AdminRepository _adminRepo;
        private readonly StudentRepository _studentRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly PasswordService _pwd;

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

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
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

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(AdminUpdateProfileRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var email = req.Email.Trim().ToLowerInvariant();
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

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null) return NotFound("Admin not found.");

            if (!_pwd.Verify(req.CurrentPassword, admin.PasswordHash))
                return BadRequest("Current password is incorrect.");

            var newHash = _pwd.Hash(req.NewPassword);
            var updated = await _adminRepo.UpdatePasswordHashAsync(adminId, newHash);
            if (!updated) return StatusCode(500, "Failed to update password.");

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _studentRepo.GetAllAsync();
            return Ok(students.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Email,
                s.CreatedAt
            }));
        }

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

        [HttpPatch("companies/{companyId:int}/status")]
        public async Task<IActionResult> UpdateCompanyStatus(int companyId, AdminUpdateCompanyStatusRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

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

        private bool TryGetCurrentAdminId(out int adminId)
        {
            adminId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value;
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out adminId);
        }
    }
}
