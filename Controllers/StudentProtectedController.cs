using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/student")]
    public class StudentProtectedController : ControllerBase
    {
        [Authorize(Roles = "STUDENT")]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                message = "You are authorized as STUDENT",
                userId = User.FindFirst("userId")?.Value,
                email = User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value
            });
        }
    }
}