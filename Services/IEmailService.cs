namespace PATHFINDER_BACKEND.Services
{
    public interface IEmailService
    {
        Task<bool> SendCompanyApprovalEmailAsync(string toEmail, string companyName, string status, string? rejectionReason = null);
        
        // Add this new method for password reset
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userType);
    }
}