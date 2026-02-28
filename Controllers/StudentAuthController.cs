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

        /// <summary>
        /// Registers a new student and returns a JWT token (auto-login).
        /// </summary>
        /// <response code="200">Returns token + student info.</response>
        /// <response code="409">Email already registered.</response>
        [HttpPost("register")]
        public async Task<IActionResult> Register(StudentRegisterRequest req)
        {
            // DataAnnotations validation happens here (Required, EmailAddress, MinLength).
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Normalize email to avoid duplicates like A@x.com vs a@x.com.
            var email = req.Email.Trim().ToLower();

            // Check for existing account to provide a clean, user-friendly message.
            var existing = await _repo.GetByEmailAsync(email);
            if (existing != null) return Conflict("Email already registered.");

            var student = new Student
            {
                FullName = req.FullName.Trim(),
                Email = email,

                // Password is hashed before storing (never store plain password).
                PasswordHash = _pwd.Hash(req.Password)
            };

            int id;
            try
            {
                id = await _repo.CreateAsync(student);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Handles unique constraint violations safely (race condition safe).
                return Conflict("Email already registered.");
            }

            // Auto-login after successful registration (common UX).
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

        /// <summary>
        /// Logs in a student and returns a JWT token.
        /// </summary>
        /// <response code="200">Returns token + student info.</response>
        /// <response code="401">Invalid credentials.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(StudentLoginRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = req.Email.Trim().ToLower();

            var student = await _repo.GetByEmailAsync(email);
            if (student == null)
            {
                // Generic response prevents leaking whether email exists (security best practice).
                return Unauthorized("Invalid credentials.");
            }

            if (!_pwd.Verify(req.Password, student.PasswordHash))
            {
                // Same generic response: avoids account enumeration.
                return Unauthorized("Invalid credentials.");
            }

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

        /// <summary>
        /// Logs out a student by revoking the current JWT token (via JTI claim).
        /// Since JWT is stateless, revocation is required to invalidate before expiry.
        /// </summary>
        [Authorize(Roles = "STUDENT")]
        [HttpPost("logout")]
        public IActionResult Logout([FromServices] TokenRevocationService revocationService)
        {
            var authHeader = Request.Headers.Authorization.ToString();

            // Must be in the form: "Bearer <token>"
            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("Authorization token missing.");
            }

            var token = authHeader["Bearer ".Length..].Trim();

            // We read jti + expiry so we can revoke and auto-cleanup after expiry.
            var (jti, expiresUtc) = _jwt.ReadJtiAndExpiry(token);

            if (string.IsNullOrWhiteSpace(jti) || expiresUtc == null)
                return Unauthorized("Invalid token.");

            revocationService.Revoke(jti, expiresUtc.Value.ToUniversalTime());

            return Ok(new { message = "Logged out successfully." });
        }
    }
}