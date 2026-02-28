using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/admin/auth")]
    public class AdminAuthController : ControllerBase
    {
        private readonly AdminRepository _repo;
        private readonly PasswordService _pwd;
        private readonly JwtTokenService _jwt;

        public AdminAuthController(AdminRepository repo, PasswordService pwd, JwtTokenService jwt)
        {
            _repo = repo;
            _pwd = pwd;
            _jwt = jwt;
        }

        /// <summary>
        /// Authenticates an admin user and issues a JWT token with role ADMIN.
        /// </summary>
        /// <response code="200">Returns JWT token + admin profile info.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginRequest req)
        {
            // Enforces [Required] + [EmailAddress] validations.
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Normalize email for consistent lookup.
            var email = req.Email.Trim().ToLowerInvariant();

            var admin = await _repo.GetByEmailAsync(email);
            if (admin == null) return Unauthorized("Invalid credentials.");

            // Verify password hash using BCrypt.
            if (!_pwd.Verify(req.Password, admin.PasswordHash))
                return Unauthorized("Invalid credentials.");

            // Issue JWT with ADMIN role claim (required for [Authorize(Roles="ADMIN")]).
            var token = _jwt.CreateToken(admin.Id, admin.Email, "ADMIN", admin.FullName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = admin.Id,
                Role = "ADMIN",
                Email = admin.Email,
                FullName = admin.FullName
            });
        }
    }
}