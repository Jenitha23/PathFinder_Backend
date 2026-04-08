using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PATHFINDER_BACKEND.DTOs;
using PATHFINDER_BACKEND.Models;
using PATHFINDER_BACKEND.Repositories;

namespace PATHFINDER_BACKEND.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "ADMIN")]
    public class AdminUserManagementController : ControllerBase
    {
        private readonly StudentRepository _studentRepo;
        private readonly CompanyRepository _companyRepo;
        private readonly AdminRepository _adminRepo;
        private readonly ApplicationRepository _applicationRepo;
        private readonly CompanyJobRepository _companyJobRepo;

        public AdminUserManagementController(
            StudentRepository studentRepo,
            CompanyRepository companyRepo,
            AdminRepository adminRepo,
            ApplicationRepository applicationRepo,
            CompanyJobRepository companyJobRepo)
        {
            _studentRepo = studentRepo;
            _companyRepo = companyRepo;
            _adminRepo = adminRepo;
            _applicationRepo = applicationRepo;
            _companyJobRepo = companyJobRepo;
        }

        #region ========== STUDENT MANAGEMENT ==========

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (students, totalCount) = await GetStudentsWithFilterAsync(search, status, page, pageSize);

            var studentDtos = new List<StudentListItemDto>();
            foreach (var student in students)
            {
                var applicationCount = await _applicationRepo.GetStudentApplicationCountAsync(student.Id);
                studentDtos.Add(new StudentListItemDto
                {
                    Id = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,
                    Role = "STUDENT",
                    Status = "ACTIVE", // Default status until you add the column
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = null,
                    SuspensionReason = null,
                    ApplicationsCount = applicationCount,
                    HasProfile = !string.IsNullOrWhiteSpace(student.FullName)
                });
            }

            return Ok(new AdminUserListResponse
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Users = studentDtos.Cast<object>().ToList()
            });
        }

        [HttpGet("students/{studentId:int}")]
        public async Task<IActionResult> GetStudentById(int studentId)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
                return NotFound(new { message = $"Student with ID {studentId} not found." });

            var applicationCount = await _applicationRepo.GetStudentApplicationCountAsync(studentId);

            return Ok(new
            {
                student.Id,
                student.FullName,
                student.Email,
                role = "STUDENT",
                status = "ACTIVE",
                student.CreatedAt,
                updatedAt = (DateTime?)null,
                suspensionReason = (string?)null,
                applicationsCount = applicationCount,
                hasProfile = !string.IsNullOrWhiteSpace(student.FullName)
            });
        }

        [HttpPut("students/{studentId:int}")]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] AdminUpdateStudentRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
                return NotFound(new { message = $"Student with ID {studentId} not found." });

            var normalizedEmail = req.Email.Trim().ToLowerInvariant();
            if (!string.Equals(student.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingByEmail = await _studentRepo.GetByEmailAsync(normalizedEmail);
                if (existingByEmail != null && existingByEmail.Id != studentId)
                    return Conflict(new { message = $"Email '{req.Email}' is already registered by another student." });
            }

            var updated = await _studentRepo.UpdateProfileAsync(studentId, req.FullName.Trim(), normalizedEmail);
            if (!updated)
                return StatusCode(500, new { message = "Failed to update student account." });

            await LogAuditAsync(adminId, "UPDATE_STUDENT", studentId.ToString(), student.Email, 
                $"Updated student {studentId}");

            return Ok(new
            {
                success = true,
                message = "Student account updated successfully.",
                user = new
                {
                    studentId,
                    fullName = req.FullName.Trim(),
                    email = normalizedEmail,
                    role = "STUDENT",
                    status = req.Status
                }
            });
        }

        [HttpDelete("students/{studentId:int}")]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
                return NotFound(new { message = $"Student with ID {studentId} not found." });

            var deleted = await _studentRepo.DeleteByIdAsync(studentId);
            if (!deleted)
                return StatusCode(500, new { message = "Failed to delete student account." });

            await LogAuditAsync(adminId, "DELETE_STUDENT", studentId.ToString(), student.Email, 
                $"Deleted student {studentId}: {student.FullName}");

            return Ok(new AdminDeleteResponse
            {
                Success = true,
                Message = $"Student '{student.FullName}' has been deleted successfully.",
                UserId = studentId,
                UserType = "STUDENT",
                Email = student.Email,
                Name = student.FullName,
                IsSoftDelete = false,
                DeletedAt = DateTime.UtcNow
            });
        }

        #endregion

        #region ========== COMPANY MANAGEMENT ==========

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (companies, totalCount) = await GetCompaniesWithFilterAsync(search, status, page, pageSize);

            var companyDtos = new List<CompanyListItemDto>();
            foreach (var company in companies)
            {
                var jobs = await _companyJobRepo.GetJobsByCompanyIdAsync(company.Id);
                companyDtos.Add(new CompanyListItemDto
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    Email = company.Email,
                    Role = "COMPANY",
                    Status = company.Status,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    SuspensionReason = null,
                    Industry = company.Industry,
                    LogoUrl = company.LogoUrl,
                    JobsCount = jobs.Count,
                    ApplicationsCount = 0,
                    HasCompleteProfile = !string.IsNullOrWhiteSpace(company.Description) && 
                                        !string.IsNullOrWhiteSpace(company.Industry)
                });
            }

            return Ok(new AdminUserListResponse
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Users = companyDtos.Cast<object>().ToList()
            });
        }

        [HttpGet("companies/{companyId:int}")]
        public async Task<IActionResult> GetCompanyById(int companyId)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = $"Company with ID {companyId} not found." });

            var jobs = await _companyJobRepo.GetJobsByCompanyIdAsync(companyId);

            return Ok(new
            {
                company.Id,
                company.CompanyName,
                company.Email,
                role = "COMPANY",
                company.Status,
                company.CreatedAt,
                company.UpdatedAt,
                company.Description,
                company.Industry,
                company.Website,
                company.Location,
                company.Phone,
                company.LogoUrl,
                company.RejectionReason,
                company.ApprovedAt,
                company.AdminNotes,
                jobsCount = jobs.Count,
                hasCompleteProfile = !string.IsNullOrWhiteSpace(company.Description) && 
                                    !string.IsNullOrWhiteSpace(company.Industry)
            });
        }

        [HttpPut("companies/{companyId:int}")]
        public async Task<IActionResult> UpdateCompany(int companyId, [FromBody] AdminUpdateCompanyRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = $"Company with ID {companyId} not found." });

            var normalizedEmail = req.Email.Trim().ToLowerInvariant();
            if (!string.Equals(company.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingByEmail = await _companyRepo.GetByEmailAsync(normalizedEmail);
                if (existingByEmail != null && existingByEmail.Id != companyId)
                    return Conflict(new { message = $"Email '{req.Email}' is already registered by another company." });
            }

            if (!IsValidCompanyStatusTransition(company.Status, req.Status))
            {
                return BadRequest(new
                {
                    message = $"Cannot change company status from '{company.Status}' to '{req.Status}'."
                });
            }

            var updated = await _companyRepo.UpdateProfileAsync(companyId, req.CompanyName.Trim(), normalizedEmail);
            if (!updated)
                return StatusCode(500, new { message = "Failed to update company account." });

            // Also update status if changed
            if (company.Status != req.Status)
            {
                await _companyRepo.UpdateStatusAsync(companyId, req.Status);
            }

            await LogAuditAsync(adminId, "UPDATE_COMPANY", companyId.ToString(), company.Email, 
                $"Updated company {companyId}. Status changed from {company.Status} to {req.Status}");

            return Ok(new
            {
                success = true,
                message = "Company account updated successfully.",
                user = new
                {
                    companyId,
                    companyName = req.CompanyName.Trim(),
                    email = normalizedEmail,
                    role = "COMPANY",
                    status = req.Status
                }
            });
        }

        [HttpDelete("companies/{companyId:int}")]
        public async Task<IActionResult> DeleteCompany(int companyId)
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var company = await _companyRepo.GetByIdAsync(companyId);
            if (company == null)
                return NotFound(new { message = $"Company with ID {companyId} not found." });

            var deleted = await _companyRepo.DeleteByIdAsync(companyId);
            if (!deleted)
                return StatusCode(500, new { message = "Failed to delete company account." });

            await LogAuditAsync(adminId, "DELETE_COMPANY", companyId.ToString(), company.Email, 
                $"Deleted company {companyId}: {company.CompanyName}");

            return Ok(new AdminDeleteResponse
            {
                Success = true,
                Message = $"Company '{company.CompanyName}' has been deleted successfully.",
                UserId = companyId,
                UserType = "COMPANY",
                Email = company.Email,
                Name = company.CompanyName,
                IsSoftDelete = false,
                DeletedAt = DateTime.UtcNow
            });
        }

        #endregion

        #region ========== STATISTICS ==========

        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            if (!TryGetCurrentAdminId(out var adminId))
                return Unauthorized(new { message = "Invalid token claims." });

            var allStudents = await _studentRepo.GetAllAsync();
            var allCompanies = await _companyRepo.GetAllAsync();

            var stats = new AdminUserStatsResponse
            {
                TotalStudents = allStudents.Count,
                ActiveStudents = allStudents.Count,
                SuspendedStudents = 0,
                DeletedStudents = 0,
                
                TotalCompanies = allCompanies.Count,
                PendingCompanies = allCompanies.Count(c => c.Status == "PENDING_APPROVAL"),
                ApprovedCompanies = allCompanies.Count(c => c.Status == "APPROVED"),
                RejectedCompanies = allCompanies.Count(c => c.Status == "REJECTED"),
                SuspendedCompanies = 0,
                DeletedCompanies = 0,
                
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }

        #endregion

        #region ========== PRIVATE HELPER METHODS ==========

        private bool TryGetCurrentAdminId(out int adminId)
        {
            adminId = 0;
            var userIdClaim = User.FindFirst("userId")?.Value;
            return !string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out adminId);
        }

        private async Task<(List<Student> Students, int TotalCount)> GetStudentsWithFilterAsync(
            string? searchTerm, string? status, int page, int pageSize)
        {
            var allStudents = await _studentRepo.GetAllAsync();
            
            var filtered = allStudents.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                filtered = filtered.Where(s => 
                    s.FullName.ToLower().Contains(search) || 
                    s.Email.ToLower().Contains(search));
            }
            
            var totalCount = filtered.Count();
            var students = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            return (students, totalCount);
        }

        private async Task<(List<Company> Companies, int TotalCount)> GetCompaniesWithFilterAsync(
            string? searchTerm, string? status, int page, int pageSize)
        {
            var allCompanies = await _companyRepo.GetAllAsync();
            
            var filtered = allCompanies.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                filtered = filtered.Where(c => 
                    c.CompanyName.ToLower().Contains(search) || 
                    c.Email.ToLower().Contains(search));
            }
            
            if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
            {
                filtered = filtered.Where(c => c.Status == status);
            }
            
            var totalCount = filtered.Count();
            var companies = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            
            return (companies, totalCount);
        }

        private bool IsValidCompanyStatusTransition(string currentStatus, string newStatus)
        {
            var allowedTransitions = new Dictionary<string, HashSet<string>>
            {
                ["PENDING_APPROVAL"] = new() { "APPROVED", "REJECTED", "SUSPENDED" },
                ["APPROVED"] = new() { "SUSPENDED" },
                ["REJECTED"] = new() { "SUSPENDED" },
                ["SUSPENDED"] = new() { "APPROVED", "REJECTED" },
                ["DELETED"] = new() { }
            };

            if (!allowedTransitions.ContainsKey(currentStatus))
                return false;

            return allowedTransitions[currentStatus].Contains(newStatus);
        }

        private async Task LogAuditAsync(int adminId, string action, string? entityId, string? entityEmail, string details)
        {
            try
            {
                Console.WriteLine($"AUDIT: Admin {adminId} - {action} - {entityId} - {entityEmail} - {details}");
                await Task.CompletedTask; // This removes the warning
            }
            catch
            {
                // Don't fail the main operation
            }
        }

        #endregion
    }
}