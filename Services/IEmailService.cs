namespace PATHFINDER_BACKEND.Services
{
    public interface IEmailService
    {
        Task<bool> SendCompanyApprovalEmailAsync(string toEmail, string companyName, string status, string? rejectionReason = null);
    }
}