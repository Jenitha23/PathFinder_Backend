using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Repositories;
using PATHFINDER_BACKEND.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminProtectedController : ControllerBase
    {
        private readonly AdminRepository _adminRepo;
        private readonly StudentRepository _studentRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly CompanyProfileRepository _profileRepo;
        private readonly PasswordService _pwd;
        private readonly IEmailService? _emailService;

        private static readonly HashSet<string> AllowedCompanyStatuses =
        [
            "PENDING_APPROVAL",
            "APPROVED",
            "REJECTED"
        ];

        public AdminProtectedController(
            AdminRepository adminRepo,
            StudentRepository studentRepo,
            CompanyRepository companyRepo,
            CompanyProfileRepository profileRepo,
            PasswordService pwd,
            IEmailService? emailService = null)
        {
            _adminRepo = adminRepo;
            _studentRepo = studentRepo;
            _companyRepo = companyRepo;
            _profileRepo = profileRepo;
            _pwd = pwd;
            _emailService = emailService;
        }

        /// <summary>
        /// Returns current admin profile from DB using adminId claim.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!TryGetCurrentAdminId(out var adminId)) 
                return Unauthorized("Invalid token claims.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null) return NotFound("Admin not found.");

            return Ok(new
            {
                userId = admin.Id,
                fullName = admin.FullName,
                email = admin.Email,
                role = "ADMIN",
                createdAt = admin.CreatedAt
            });
        }

        /// <summary>
        /// Updates admin profile (full name + email).
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(AdminUpdateProfileRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var email = req.Email.Trim().ToLowerInvariant();

            var existing = await _adminRepo.GetByEmailAsync(email);
            if (existing != null && existing.Id != adminId)
                return Conflict("Email already registered.");

            var updated = await _adminRepo.UpdateProfileAsync(adminId, req.FullName.Trim(), email);
            if (!updated) return NotFound("Admin not found.");

            return Ok(new
            {
                message = "Profile updated successfully.",
                fullName = req.FullName.Trim(),
                email
            });
        }

        /// <summary>
        /// Changes admin password after verifying the current password.
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordRequest req)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!TryGetCurrentAdminId(out var adminId)) return Unauthorized("Invalid token claims.");

            var admin = await _adminRepo.GetByIdAsync(adminId);
            if (admin == null) return NotFound("Admin not found.");

            if (!_pwd.Verify(req.CurrentPassword, admin.PasswordHash))
                return BadRequest("Current password is incorrect.");

            var newHash = _pwd.Hash(req.NewPassword);
            var updated = await _adminRepo.UpdatePasswordHashAsync(adminId, newHash);
            if (!updated) return StatusCode(500, "Failed to update password.");

            return Ok(new { message = "Password changed successfully." });
        }

        /// <summary>
        /// Lists students for admin dashboard.
        /// </summary>
        [HttpGet("students")]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _studentRepo.GetAllAsync();

            return Ok(students.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Email,
                s.CreatedAt
            }));
        }

        /// <summary>
        /// Enhanced endpoint to get companies with filtering and pagination.
        /// </summary>
        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies([FromQuery] CompanyListFilterRequest filter)
        {
            if (!TryGetCurrentAdminId(out var adminId)) 
                return Unauthorized("Invalid token claims.");

            // Validate pagination
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 20;

            var (companies, totalCount) = await _companyRepo.GetCompaniesFilteredAsync(filter);

            if (companies.Count == 0)
            {
                var noDataMessage = !string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "ALL"
                    ? $"No companies found with status '{filter.Status}'."
                    : !string.IsNullOrWhiteSpace(filter.SearchTerm)
                        ? $"No companies found matching '{filter.SearchTerm}'."
                        : "No companies found.";

                return Ok(new
                {
                    message = noDataMessage,
                    total = 0,
                    page = filter.Page,
                    pageSize = filter.PageSize,
                    totalPages = 0,
                    companies = new List<CompanyListItemResponse>()
                });
            }

            return Ok(new
            {
                message = $"Found {totalCount} company(s).",
                total = totalCount,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                companies
            });
        }

        /// <summary>
        /// Get pending companies count for dashboard badge.
        /// </summary>
        [HttpGet("companies/pending/count")]
        public async Task<IActionResult> GetPendingCompaniesCount()
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized("Invalid token claims.");

            var pendingCount = await _companyRepo.GetPendingCompaniesCountAsync();

            return Ok(new
            {
                pendingCount = pendingCount,
                hasPending = pendingCount > 0,
                message = pendingCount > 0 
                    ? $"You have {pendingCount} company(s) pending approval." 
                    : "No pending companies. All caught up!"
            });
        }

        /// <summary>
        /// Get company details for review (full profile).
        /// </summary>
        [HttpGet("companies/{companyId:int}/review")]
        public async Task<IActionResult> GetCompanyForReview(int companyId)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized("Invalid token claims.");

            var company = await _profileRepo.GetCompanyProfileAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            var jobCount = await _companyRepo.GetCompanyJobCountAsync(companyId);
            var auditLogs = await _companyRepo.GetCompanyAuditLogsAsync(companyId, 10);
            var hasLogo = !string.IsNullOrWhiteSpace(company.LogoUrl);

            var reviewGuidance = GetReviewGuidance(company);

            return Ok(new
            {
                company = new
                {
                    company.Id,
                    company.CompanyName,
                    company.Email,
                    company.Description,
                    company.Industry,
                    company.Website,
                    company.Location,
                    company.Phone,
                    company.LogoUrl,
                    company.Status,
                    company.CreatedAt,
                    company.RejectionReason,
                    company.AdminNotes,
                    ApprovedAt = company.ApprovedAt
                },
                stats = new
                {
                    totalJobsPosted = jobCount,
                    hasCompleteProfile = !string.IsNullOrWhiteSpace(company.Description) && 
                                          !string.IsNullOrWhiteSpace(company.Industry),
                    hasLogo = hasLogo,
                    profileCompleteness = CalculateProfileCompleteness(company)
                },
                reviewGuidance = reviewGuidance,
                auditLogs = auditLogs.Select(l => new
                {
                    l.Action,
                    l.OldValue,
                    l.NewValue,
                    l.Details,
                    l.CreatedAt
                }),
                actions = new
                {
                    canApprove = company.Status == "PENDING_APPROVAL",
                    canReject = company.Status == "PENDING_APPROVAL",
                    canReconsider = company.Status == "REJECTED"
                }
            });
        }

        /// <summary>
        /// Enhanced endpoint to update company status with rejection reason and audit.
        /// </summary>
        [HttpPatch("companies/{companyId:int}/status")]
        public async Task<IActionResult> UpdateCompanyStatus(int companyId, AdminUpdateCompanyStatusRequest req)
        {
            if (!ModelState.IsValid) 
                return ValidationProblem(ModelState);

            if (!TryGetCurrentAdminId(out var adminId)) 
                return Unauthorized("Invalid token claims.");

            var status = req.Status.Trim().ToUpperInvariant();

            if (!AllowedCompanyStatuses.Contains(status))
                return BadRequest("Status must be one of: PENDING_APPROVAL, APPROVED, REJECTED.");

            // Validate rejection reason is provided when rejecting
            if (status == "REJECTED" && string.IsNullOrWhiteSpace(req.RejectionReason))
                return BadRequest(new 
                { 
                    message = "Rejection reason is required when rejecting a company.",
                    field = "rejectionReason"
                });

            // Get company details before update for response
            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            var oldStatus = company.Status;

            // Perform update with audit
            var (success, message) = await _companyRepo.UpdateCompanyStatusWithAuditAsync(
                companyId: companyId,
                status: status,
                adminId: adminId,
                rejectionReason: req.RejectionReason,
                adminNotes: req.AdminNotes
            );

            if (!success)
                return StatusCode(500, new { message });

            // Send email notification if requested and email service is available
            bool emailSent = false;
            if (req.SendEmailNotification && _emailService != null)
            {
                try
                {
                    emailSent = await _emailService.SendCompanyApprovalEmailAsync(
                        company.Email, 
                        company.CompanyName, 
                        status, 
                        req.RejectionReason
                    );
                }
                catch
                {
                    // Log error but don't fail the operation
                    emailSent = false;
                }
            }

            // Get admin name for response
            var admin = await _adminRepo.GetByIdAsync(adminId);
            var adminName = admin?.FullName;

            var response = new CompanyApprovalResponse
            {
                CompanyId = companyId,
                CompanyName = company.CompanyName,
                Email = company.Email,
                OldStatus = oldStatus,
                NewStatus = status,
                RejectionReason = req.RejectionReason,
                ApprovedBy = adminId,
                ApprovedByName = adminName,
                ApprovedAt = DateTime.UtcNow,
                Message = message,
                EmailSent = emailSent
            };

            return Ok(response);
        }

        /// <summary>
        /// Bulk approve/reject multiple companies.
        /// </summary>
        [HttpPatch("companies/bulk-status")]
        public async Task<IActionResult> BulkUpdateCompanyStatus(BulkCompanyStatusUpdateRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized("Invalid token claims.");

            if (req.CompanyIds == null || !req.CompanyIds.Any())
                return BadRequest(new { message = "No company IDs provided." });

            if (req.CompanyIds.Count > 50)
                return BadRequest(new { message = "Maximum 50 companies can be processed at once." });

            var status = req.Status.Trim().ToUpperInvariant();
            if (!AllowedCompanyStatuses.Contains(status) || status == "PENDING_APPROVAL")
                return BadRequest("Bulk operations only support APPROVED or REJECTED status.");

            // Validate rejection reason for bulk reject
            if (status == "REJECTED" && string.IsNullOrWhiteSpace(req.DefaultRejectionReason))
                return BadRequest(new
                {
                    message = "Default rejection reason is required when rejecting multiple companies.",
                    field = "defaultRejectionReason"
                });

            var (successCount, failCount, results) = await _companyRepo.BulkUpdateCompanyStatusAsync(
                companyIds: req.CompanyIds,
                status: status,
                adminId: adminId,
                defaultRejectionReason: req.DefaultRejectionReason,
                adminNotes: req.AdminNotes
            );

            // Send email notifications if requested
            if (req.SendEmailNotifications && _emailService != null)
            {
                foreach (var result in results.Where(r => r.Success))
                {
                    var company = await _companyRepo.GetByIdAsync(result.CompanyId);
                    if (company != null)
                    {
                        await _emailService.SendCompanyApprovalEmailAsync(
                            company.Email,
                            company.CompanyName,
                            status,
                            status == "REJECTED" ? req.DefaultRejectionReason : null
                        );
                    }
                }
            }

            return Ok(new
            {
                message = $"Processed {req.CompanyIds.Count} companies. Success: {successCount}, Failed: {failCount}",
                successCount = successCount,
                failCount = failCount,
                results = results
            });
        }

        /// <summary>
        /// Get company audit logs.
        /// </summary>
        [HttpGet("companies/{companyId:int}/audit-logs")]
        public async Task<IActionResult> GetCompanyAuditLogs(int companyId, [FromQuery] int limit = 50)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized("Invalid token claims.");

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = "Company not found." });

            var logs = await _companyRepo.GetCompanyAuditLogsAsync(companyId, limit);

            return Ok(new
            {
                companyId = companyId,
                companyName = company.CompanyName,
                totalLogs = logs.Count,
                logs = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.OldValue,
                    l.NewValue,
                    l.Details,
                    l.CreatedAt
                })
            });
        }

        /// <summary>
        /// Deletes currently authenticated admin account.
        /// </summary>
        [HttpDelete("account")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            if (!TryGetCurrentAdminId(out var adminId)) 
                return Unauthorized("Invalid token claims.");

            var deleted = await _adminRepo.DeleteByIdAsync(adminId);
            if (!deleted) return NotFound("Admin not found.");

            return Ok(new { message = "Admin account deleted successfully." });
        }

        // Private helper methods
        private bool TryGetCurrentAdminId(out int adminId)
        {
            adminId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value;
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out adminId);
        }

        private object GetReviewGuidance(Models.Company company)
        {
            var guidance = new List<string>();
            var recommendations = new List<string>();

            if (string.IsNullOrWhiteSpace(company.Description))
            {
                guidance.Add("⚠️ Company has no description provided.");
                recommendations.Add("Request company description");
            }

            if (string.IsNullOrWhiteSpace(company.Industry))
            {
                guidance.Add("⚠️ Industry not specified.");
                recommendations.Add("Request industry information");
            }

            if (string.IsNullOrWhiteSpace(company.Website))
            {
                guidance.Add("⚠️ Website URL missing.");
                recommendations.Add("Request company website");
            }

            if (string.IsNullOrWhiteSpace(company.Location))
            {
                guidance.Add("⚠️ Location not specified.");
                recommendations.Add("Request office location");
            }

            if (string.IsNullOrWhiteSpace(company.Phone))
            {
                guidance.Add("ℹ️ Phone number not provided (optional).");
            }

            if (string.IsNullOrWhiteSpace(company.LogoUrl))
            {
                guidance.Add("ℹ️ Company logo not uploaded (recommended for branding).");
                recommendations.Add("Encourage logo upload");
            }

            var isComplete = guidance.Count(g => g.Contains("⚠️")) == 0;

            return new
            {
                isComplete = isComplete,
                issues = guidance,
                recommendations = recommendations,
                summary = isComplete 
                    ? "✅ This company profile looks complete and ready for approval." 
                    : "⚠️ This company profile has missing information. Consider requesting additional details before approval.",
                actionSuggestion = isComplete 
                    ? "Ready to approve" 
                    : "Review missing information before approving"
            };
        }

        private int CalculateProfileCompleteness(Models.Company company)
        {
            int score = 0;
            int totalFields = 7; // Description, Industry, Website, Location, Phone, LogoUrl, Email verified

            if (!string.IsNullOrWhiteSpace(company.Description)) score++;
            if (!string.IsNullOrWhiteSpace(company.Industry)) score++;
            if (!string.IsNullOrWhiteSpace(company.Website)) score++;
            if (!string.IsNullOrWhiteSpace(company.Location)) score++;
            if (!string.IsNullOrWhiteSpace(company.Phone)) score++;
            if (!string.IsNullOrWhiteSpace(company.LogoUrl)) score++;
            if (!string.IsNullOrWhiteSpace(company.Email)) score++;

            return (int)Math.Round((score / (double)totalFields) * 100);
        }
    }
}