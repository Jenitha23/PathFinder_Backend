using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Services;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly PasswordResetService _passwordResetService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(
            PasswordResetService passwordResetService,
            ILogger<PasswordResetController> logger)
        {
            _passwordResetService = passwordResetService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/passwordreset/forgot
        /// Sends a password reset link to the user's email
        /// </summary>
        [HttpPost("forgot")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _passwordResetService.ForgotPasswordAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            
            return Ok(new { 
                message = result.Message,
                expiresAt = result.ExpiresAt
            });
        }

        /// <summary>
        /// POST /api/passwordreset/reset
        /// Resets the user's password using a valid token
        /// </summary>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _passwordResetService.ResetPasswordAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            
            return Ok(new { message = result.Message });
        }

        /// <summary>
        /// POST /api/passwordreset/validate-token
        /// Validates if a reset token is still valid (for frontend)
        /// </summary>
        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Ok(new { valid = false, message = "Token is required" });
            }

            var resetToken = await _passwordResetService.ValidateTokenAsync(request.Token);
            
            if (resetToken == null)
            {
                return Ok(new { valid = false, message = "Token is invalid or expired" });
            }
            
            return Ok(new { 
                valid = true, 
                email = resetToken.Email,
                userType = resetToken.UserType,
                expiresAt = resetToken.ExpiresAt
            });
        }
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = "";
    }
}