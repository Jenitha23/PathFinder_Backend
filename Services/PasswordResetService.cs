using System.Security.Cryptography;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.Repositories;

namespace PATHFINDER_BACKEND.Services
{
    public class PasswordResetService
    {
        private readonly PasswordResetRepository _resetRepo;
        private readonly StudentRepository _studentRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly PasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            PasswordResetRepository resetRepo,
            StudentRepository studentRepo,
            CompanyRepository companyRepo,
            PasswordService passwordService,
            IEmailService emailService,
            ILogger<PasswordResetService> logger)
        {
            _resetRepo = resetRepo;
            _studentRepo = studentRepo;
            _companyRepo = companyRepo;
            _passwordService = passwordService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PasswordResetResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var email = request.Email.Trim().ToLowerInvariant();
                var userType = request.UserType.ToUpperInvariant();
                
                // Verify user exists
                bool userExists = false;
                if (userType == "STUDENT")
                {
                    var student = await _studentRepo.GetByEmailAsync(email);
                    userExists = student != null;
                }
                else if (userType == "COMPANY")
                {
                    var company = await _companyRepo.GetByEmailAsync(email);
                    userExists = company != null;
                }
                else
                {
                    return new PasswordResetResponse
                    {
                        Success = false,
                        Message = "Invalid user type"
                    };
                }

                if (!userExists)
                {
                    // Don't reveal that user doesn't exist for security reasons
                    _logger.LogWarning($"Password reset requested for non-existent email: {email}");
                    return new PasswordResetResponse
                    {
                        Success = true, // Still return true to prevent email enumeration
                        Message = "If an account exists with this email, you will receive a password reset link."
                    };
                }

                // Generate secure token
                var token = GenerateSecureToken();
                
                // Invalidate old tokens for this email
                await _resetRepo.InvalidateAllTokensForEmailAsync(email, userType);
                
                // Save new token
                var resetToken = new PasswordResetToken
                {
                    Email = email,
                    Token = token,
                    UserType = userType,
                    Used = false,
                    ExpiresAt = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
                    CreatedAt = DateTime.UtcNow
                };
                await _resetRepo.SaveResetTokenAsync(resetToken);
                
                // Send email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(email, token, userType);
                
                if (!emailSent)
                {
                    _logger.LogError($"Failed to send password reset email to {email}");
                    return new PasswordResetResponse
                    {
                        Success = false,
                        Message = "Failed to send reset email. Please try again later."
                    };
                }
                
                return new PasswordResetResponse
                {
                    Success = true,
                    Message = "Password reset link has been sent to your email.",
                    ResetToken = token, // Only for testing - remove in production
                    ExpiresAt = resetToken.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password process");
                return new PasswordResetResponse
                {
                    Success = false,
                    Message = "An error occurred. Please try again later."
                };
            }
        }

        public async Task<PasswordResetResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                // Validate token
                var resetToken = await _resetRepo.GetValidTokenAsync(request.Token);
                
                if (resetToken == null)
                {
                    return new PasswordResetResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token. Please request a new password reset."
                    };
                }
                
                // Hash new password
                var newPasswordHash = _passwordService.Hash(request.NewPassword);
                
                // Update password based on user type
                bool passwordUpdated = false;
                if (resetToken.UserType == "STUDENT")
                {
                    var student = await _studentRepo.GetByEmailAsync(resetToken.Email);
                    if (student != null)
                    {
                        passwordUpdated = await _studentRepo.UpdatePasswordHashAsync(student.Id, newPasswordHash);
                    }
                }
                else if (resetToken.UserType == "COMPANY")
                {
                    var company = await _companyRepo.GetByEmailAsync(resetToken.Email);
                    if (company != null)
                    {
                        passwordUpdated = await _companyRepo.UpdatePasswordHashAsync(company.Id, newPasswordHash);
                    }
                }
                
                if (!passwordUpdated)
                {
                    return new PasswordResetResponse
                    {
                        Success = false,
                        Message = "Failed to reset password. User not found."
                    };
                }
                
                // Mark token as used
                await _resetRepo.MarkTokenAsUsedAsync(resetToken.Id);
                
                // Invalidate all other tokens for this email
                await _resetRepo.InvalidateAllTokensForEmailAsync(resetToken.Email, resetToken.UserType);
                
                _logger.LogInformation($"Password reset successfully for {resetToken.Email} ({resetToken.UserType})");
                
                return new PasswordResetResponse
                {
                    Success = true,
                    Message = "Password has been reset successfully. You can now log in with your new password."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reset password process");
                return new PasswordResetResponse
                {
                    Success = false,
                    Message = "An error occurred. Please try again later."
                };
            }
        }

        /// <summary>
        /// Validates if a reset token is still valid (for frontend)
        /// </summary>
        public async Task<PasswordResetToken?> ValidateTokenAsync(string token)
        {
            try
            {
                return await _resetRepo.GetValidTokenAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return null;
            }
        }

        private string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}