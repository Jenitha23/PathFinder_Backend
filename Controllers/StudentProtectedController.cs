using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student")]
    public class StudentProtectedController : ControllerBase
    {
        /// <summary>
        /// Sample protected endpoint.
        /// Demonstrates role-based authorization using JWT role claim.
        /// </summary>
        [Authorize(Roles = "STUDENT")]
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
    }
}