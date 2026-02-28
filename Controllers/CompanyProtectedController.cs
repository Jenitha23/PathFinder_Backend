using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company")]
    [Authorize(Roles = "COMPANY")]
    public class CompanyProtectedController : ControllerBase
    {
        private readonly CompanyRepository _repo;

        public CompanyProtectedController(CompanyRepository repo)
        {
            _repo = repo;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Protected endpoint accessible only to authenticated COMPANY role.
        /// Demonstrates role-based authorization using JWT.
        /// </summary>
        [Authorize(Roles = "COMPANY")]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                message = "You are authorized as COMPANY",

                // Retrieved from custom JWT claims
                userId = User.FindFirst("userId")?.Value,
                email = User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value,
                name = User.Claims.FirstOrDefault(c => c.Type == "fullName")?.Value
            });
        }

        /// <summary>
        /// Updates current company profile (company name + email).
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateCompanyProfileRequest req)
        {
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized("Invalid token claims.");
            if (string.IsNullOrWhiteSpace(req.CompanyName) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("CompanyName and Email are required.");

            var email = req.Email.Trim().ToLowerInvariant();
            if (!IsValidEmail(email)) return BadRequest("Invalid email format.");

            var existing = await _repo.GetByEmailAsync(email);
            if (existing != null && existing.Id != companyId)
                return Conflict("Email already registered.");

            var updated = await _repo.UpdateProfileAsync(companyId, req.CompanyName.Trim(), email);
            if (!updated) return NotFound("Company not found.");

            return Ok(new
            {
                message = "Profile updated successfully.",
                companyName = req.CompanyName.Trim(),
                email
            });
        }

        /// <summary>
        /// Deletes current company account.
        /// </summary>
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!TryGetCurrentCompanyId(out var companyId)) return Unauthorized("Invalid token claims.");

            var deleted = await _repo.DeleteByIdAsync(companyId);
            if (!deleted) return NotFound("Company not found.");

            return Ok(new { message = "Company account deleted successfully." });
        }

        private bool TryGetCurrentCompanyId(out int companyId)
        {
            companyId = 0;

            var userId = User.FindFirst("userId")?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out companyId);
        }

        public class UpdateCompanyProfileRequest
        {
            public string CompanyName { get; set; } = "";
            public string Email { get; set; } = "";
        }
    }
}
