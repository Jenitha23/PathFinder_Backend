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

        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = req.Email.Trim().ToLowerInvariant();
            var admin = await _repo.GetByEmailAsync(email);
            if (admin == null) return Unauthorized("Invalid credentials.");

            if (!_pwd.Verify(req.Password, admin.PasswordHash))
                return Unauthorized("Invalid credentials.");

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
