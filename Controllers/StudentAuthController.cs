using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student/auth")]
    public class StudentAuthController : ControllerBase
    {
        private readonly StudentRepository _repo;
        private readonly PasswordService _pwd;
        private readonly JwtTokenService _jwt;

        public StudentAuthController(StudentRepository repo, PasswordService pwd, JwtTokenService jwt)
        {
            _repo = repo;
            _pwd = pwd;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(StudentRegisterRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(req.FullName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("FullName, Email and Password are required.");

            var email = req.Email.Trim().ToLower();
            var existing = await _repo.GetByEmailAsync(email);
            if (existing != null) return Conflict("Email already registered.");

            var student = new Student
            {
                FullName = req.FullName.Trim(),
                Email = email,
                PasswordHash = _pwd.Hash(req.Password)
            };

            int id;
            try
            {
                id = await _repo.CreateAsync(student);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return Conflict("Email already registered.");
            }

            // optionally auto-login after register:
            var token = _jwt.CreateToken(id, email, "STUDENT", student.FullName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = id,
                Role = "STUDENT",
                Email = email,
                FullName = student.FullName
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(StudentLoginRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Email and Password are required.");

            var email = req.Email.Trim().ToLower();
            var student = await _repo.GetByEmailAsync(email);
            if (student == null) return Unauthorized("Invalid credentials.");

            if (!_pwd.Verify(req.Password, student.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = _jwt.CreateToken(student.Id, student.Email, "STUDENT", student.FullName);

            return Ok(new AuthResponse
            {
                Token = token,
                UserId = student.Id,
                Role = "STUDENT",
                Email = student.Email,
                FullName = student.FullName
            });
        }

        [Authorize(Roles = "STUDENT")]
        [HttpPost("logout")]
        public IActionResult Logout([FromServices] TokenRevocationService revocationService)
        {
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Authorization token missing.");

            var token = authHeader["Bearer ".Length..].Trim();
            var (jti, expiresUtc) = _jwt.ReadJtiAndExpiry(token);

            if (string.IsNullOrWhiteSpace(jti) || expiresUtc == null)
                return Unauthorized("Invalid token.");

            revocationService.Revoke(jti, expiresUtc.Value.ToUniversalTime());
            return Ok(new { message = "Logged out successfully." });
        }
    }
}
