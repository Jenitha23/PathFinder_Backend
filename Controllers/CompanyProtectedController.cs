using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/company")]
    public class CompanyProtectedController : ControllerBase
    {
        [Authorize(Roles = "COMPANY")]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                message = "You are authorized as COMPANY",
                userId = User.FindFirst("userId")?.Value,
                email = User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value,
                name = User.Claims.FirstOrDefault(c => c.Type == "fullName")?.Value
            });
        }
    }
}