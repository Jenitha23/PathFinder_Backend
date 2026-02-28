using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company")]
    public class CompanyProtectedController : ControllerBase
    {
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
    }
}