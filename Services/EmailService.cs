using System.Net;
using System.Net.Mail;

namespace PATHFINDER_BACKEND.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Read SMTP settings from configuration
            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["Email:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string userType)
        {
            try
            {
                var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
                var resetLink = $"{frontendUrl}/reset-password?token={resetToken}&type={userType.ToLower()}";
                
                var subject = "Password Reset Request - PathFinder";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #3B82F6; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .button {{ background-color: #3B82F6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                            .footer {{ font-size: 12px; color: #666; text-align: center; margin-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>PathFinder - Password Reset</h2>
                            </div>
                            <div class='content'>
                                <p>Hello,</p>
                                <p>We received a request to reset your password for your {userType} account.</p>
                                <p>Click the button below to reset your password:</p>
                                <p style='text-align: center;'>
                                    <a href='{resetLink}' class='button'>Reset Password</a>
                                </p>
                                <p>Or copy this link: <br/>{resetLink}</p>
                                <p>This link will expire in <strong>1 hour</strong>.</p>
                                <p>If you didn't request this, please ignore this email.</p>
                                <hr/>
                                <p><strong>Security Notice:</strong> Never share this link with anyone.</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2024 PathFinder. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                using var client = new SmtpClient(_smtpHost, _smtpPort);
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, "PathFinder Support"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Password reset email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {toEmail}");
                return false;
            }
        }

        public async Task<bool> SendCompanyApprovalEmailAsync(string toEmail, string companyName, string status, string? rejectionReason = null)
        {
            try
            {
                var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
                var subject = status == "APPROVED" 
                    ? "Your Company Registration has been Approved - PathFinder" 
                    : "Your Company Registration Status - PathFinder";
                
                var body = status == "APPROVED"
                    ? $@"
                        <html>
                        <body>
                            <h2>Congratulations, {companyName}!</h2>
                            <p>Your company registration has been <strong>approved</strong>.</p>
                            <p>You can now log in to your account and start posting jobs.</p>
                            <p><a href='{frontendUrl}/company/login'>Click here to login</a></p>
                        </body>
                        </html>"
                    : $@"
                        <html>
                        <body>
                            <h2>Company Registration Update - {companyName}</h2>
                            <p>Your company registration has been <strong>rejected</strong>.</p>
                            <p>Reason: {rejectionReason ?? "Not specified"}</p>
                            <p>Please contact support for more information.</p>
                        </body>
                        </html>";

                using var client = new SmtpClient(_smtpHost, _smtpPort);
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, "PathFinder Support"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Company approval email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send company approval email to {toEmail}");
                return false;
            }
        }
    }
}