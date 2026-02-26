using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;
using System.Text.RegularExpressions;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company/auth")]
    public class CompanyAuthController : ControllerBase
    {
        private readonly CompanyRepository _repo;
        private readonly PasswordService _pwd;
        private readonly JwtTokenService _jwt;

        public CompanyAuthController(CompanyRepository repo, PasswordService pwd, JwtTokenService jwt)
        {
            _repo = repo;
            _pwd = pwd;
            _jwt = jwt;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CompanyRegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CompanyName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("CompanyName, Email and Password are required.");

            var email = req.Email.Trim().ToLower();
            if (!IsValidEmail(email))
                return BadRequest("Invalid email format.");

            var existing = await _repo.GetByEmailAsync(email);
            if (existing != null) return Conflict("Email already registered.");

            var company = new Company
            {
                CompanyName = req.CompanyName.Trim(),
                Email = email,
                PasswordHash = _pwd.Hash(req.Password),
                Status = "PENDING_APPROVAL"
            };

            var id = await _repo.CreateAsync(company);
            if (id <= 0) return StatusCode(500, "Failed to create company.");

            // Approval workflow: don't issue token until approved
            return Ok(new
            {
                message = "Company registered successfully. Waiting for admin approval.",
                companyId = id,
                status = company.Status,
                email = company.Email,
                companyName = company.CompanyName
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(CompanyLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Email and Password are required.");

            var email = req.Email.Trim().ToLower();
            var company = await _repo.GetByEmailAsync(email);
            if (company == null) return Unauthorized("Invalid credentials.");

            if (!_pwd.Verify(req.Password, company.PasswordHash))
                return Unauthorized("Invalid credentials.");

            // Block login until approved
            if (!string.Equals(company.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
                return Unauthorized($"Company account is not approved yet. Current status: {company.Status}");

            var token = _jwt.CreateToken(company.Id, company.Email, "COMPANY", company.CompanyName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = company.Id,
                Role = "COMPANY",
                Email = company.Email,
                FullName = company.CompanyName
            });
        }
    }
}