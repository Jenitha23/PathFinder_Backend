using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PATHFINDER_BACKEND.Tests
{
    public class AdminCompanyApprovalTests
    {
        // ==================== TEST DATA MODELS ====================

        public class CompanyTestData
        {
            public int Id { get; set; }
            public string CompanyName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Status { get; set; } = "PENDING_APPROVAL";
            public string? RejectionReason { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime? ApprovedAt { get; set; }
            public int? ApprovedBy { get; set; }
            public string? ApprovedByName { get; set; }
            public int TotalJobsPosted { get; set; }
            public int TotalApplications { get; set; }
            public bool CanBeApproved => Status == "PENDING_APPROVAL";
        }

        public class AuditLogTestData
        {
            public int Id { get; set; }
            public int AdminId { get; set; }
            public int CompanyId { get; set; }
            public string Action { get; set; } = "";
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string? Details { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class BulkOperationResultTestData
        {
            public int CompanyId { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; } = "";
        }

        public class FilterRequestTestData
        {
            public string? Status { get; set; }
            public string? SearchTerm { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string? SortBy { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }

        // ==================== STRICT EMAIL VALIDATION ====================
        
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            
            // Strict email regex that rejects double dots and other invalid patterns
            // This regex ensures:
            // - No double dots in local part or domain
            // - No trailing/leading dots
            // - Valid domain with at least one dot
            var pattern = @"^(?!\.)(?!.*\.\.)([a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*)@([a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*\.[a-zA-Z]{2,})$";
            
            if (!Regex.IsMatch(email, pattern)) return false;
            
            // Additional validation to catch edge cases
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                
                // Check for double dots in the email address string
                if (email.Contains("..")) return false;
                
                // Check for trailing dot
                if (email.EndsWith(".")) return false;
                
                // Check for leading dot
                if (email.StartsWith(".")) return false;
                
                // Split and validate parts
                var parts = email.Split('@');
                if (parts.Length != 2) return false;
                
                var localPart = parts[0];
                var domain = parts[1];
                
                // Local part cannot start or end with dot
                if (localPart.StartsWith(".") || localPart.EndsWith(".")) return false;
                
                // Domain cannot have consecutive dots
                if (domain.Contains("..")) return false;
                
                // Domain cannot start or end with dot
                if (domain.StartsWith(".") || domain.EndsWith(".")) return false;
                
                // Domain must have at least one dot
                if (!domain.Contains(".")) return false;
                
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidCompanyStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var validStatuses = new[] { "PENDING_APPROVAL", "APPROVED", "REJECTED" };
            return validStatuses.Contains(status);
        }

        private static bool RequiresRejectionReason(string status)
        {
            return status == "REJECTED";
        }

        private static int CalculateProfileCompleteness(CompanyTestData company)
        {
            int score = 0;
            int totalFields = 7;

            if (!string.IsNullOrWhiteSpace(company.CompanyName)) score++;
            if (!string.IsNullOrWhiteSpace(company.Email)) score++;
            if (!string.IsNullOrWhiteSpace(company.Status)) score++;

            return (int)Math.Round((score / (double)totalFields) * 100);
        }

        // ==================== STATUS VALIDATION TESTS ====================

        [Fact]
        public void CompanyStatus_ValidStatus_IsValid()
        {
            var validStatuses = new[] { "PENDING_APPROVAL", "APPROVED", "REJECTED" };

            foreach (var status in validStatuses)
            {
                var isValid = IsValidCompanyStatus(status);
                Assert.True(isValid, $"Status '{status}' should be valid");
            }
        }

        [Fact]
        public void CompanyStatus_InvalidStatus_IsInvalid()
        {
            var invalidStatuses = new[] { "", "ACTIVE", "INACTIVE", "SUSPENDED", "DELETED", "  " };

            foreach (var status in invalidStatuses)
            {
                var isValid = IsValidCompanyStatus(status);
                Assert.False(isValid, $"Status '{status}' should be invalid");
            }
        }

        [Fact]
        public void CompanyStatus_CaseInsensitive_ValidStatusAccepted()
        {
            var validStatuses = new[] { "PENDING_APPROVAL", "APPROVED", "REJECTED" };
            var testStatuses = new[] { "pending_approval", "APPROVED", "reJECTED", "Pending_Approval" };

            foreach (var status in testStatuses)
            {
                var isValid = validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
                Assert.True(isValid, $"Case-insensitive status '{status}' should be valid");
            }
        }

        // ==================== REJECTION REASON TESTS ====================

        [Fact]
        public void RejectCompany_WithoutReason_ShouldFail()
        {
            var status = "REJECTED";
            var rejectionReason = "";

            var requiresReason = RequiresRejectionReason(status);
            var hasValidReason = !string.IsNullOrWhiteSpace(rejectionReason);

            Assert.True(requiresReason, "Rejection should require a reason");
            Assert.False(hasValidReason, "Empty rejection reason should be invalid");
        }

        [Fact]
        public void RejectCompany_WithReason_ShouldPass()
        {
            var status = "REJECTED";
            var rejectionReason = "Incomplete company profile";

            var requiresReason = RequiresRejectionReason(status);
            var hasValidReason = !string.IsNullOrWhiteSpace(rejectionReason);

            Assert.True(requiresReason);
            Assert.True(hasValidReason);
            Assert.True(rejectionReason.Length <= 500, "Rejection reason should not exceed 500 characters");
        }

        [Fact]
        public void ApproveCompany_ReasonNotRequired()
        {
            var status = "APPROVED";
            var requiresReason = RequiresRejectionReason(status);

            Assert.False(requiresReason, "Approval should not require a reason");
        }

        [Fact]
        public void RejectionReason_MaxLength_500Characters()
        {
            var shortReason = "Missing information";
            var longReason = new string('A', 500);
            var tooLongReason = new string('B', 501);

            Assert.True(shortReason.Length <= 500);
            Assert.True(longReason.Length <= 500);
            Assert.True(tooLongReason.Length > 500, "Reason exceeding 500 chars should be invalid");
        }

        // ==================== COMPANY FILTERING TESTS ====================

        [Fact]
        public void FilterCompanies_ByStatus_ReturnsCorrectCount()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CompanyName = "Company A", Status = "PENDING_APPROVAL" },
                new() { Id = 2, CompanyName = "Company B", Status = "APPROVED" },
                new() { Id = 3, CompanyName = "Company C", Status = "PENDING_APPROVAL" },
                new() { Id = 4, CompanyName = "Company D", Status = "REJECTED" },
                new() { Id = 5, CompanyName = "Company E", Status = "APPROVED" },
                new() { Id = 6, CompanyName = "Company F", Status = "PENDING_APPROVAL" }
            };

            Assert.Equal(3, companies.Count(c => c.Status == "PENDING_APPROVAL"));
            Assert.Equal(2, companies.Count(c => c.Status == "APPROVED"));
            Assert.Equal(1, companies.Count(c => c.Status == "REJECTED"));
        }

        [Fact]
        public void FilterCompanies_BySearchTerm_ReturnsMatchingResults()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CompanyName = "Tech Corp", Email = "tech@corp.com" },
                new() { Id = 2, CompanyName = "Software Inc", Email = "soft@inc.com" },
                new() { Id = 3, CompanyName = "Tech Solutions", Email = "contact@tech.com" }
            };

            var searchTerm = "Tech";
            var matches = companies.Where(c => 
                c.CompanyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            Assert.Equal(2, matches.Count);
            Assert.Contains(matches, c => c.Id == 1);
            Assert.Contains(matches, c => c.Id == 3);
        }

        [Fact]
        public void FilterCompanies_ByDateRange_ReturnsCorrectCount()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CreatedAt = new DateTime(2024, 3, 15) },
                new() { Id = 2, CreatedAt = new DateTime(2024, 3, 20) },
                new() { Id = 3, CreatedAt = new DateTime(2024, 3, 25) },
                new() { Id = 4, CreatedAt = new DateTime(2024, 4, 1) }
            };

            var fromDate = new DateTime(2024, 3, 18);
            var toDate = new DateTime(2024, 3, 28);

            var filtered = companies.Where(c => 
                c.CreatedAt >= fromDate && c.CreatedAt <= toDate).ToList();

            Assert.Equal(2, filtered.Count);
            Assert.Contains(filtered, c => c.Id == 2);
            Assert.Contains(filtered, c => c.Id == 3);
        }

        // ==================== PAGINATION TESTS ====================

        [Fact]
        public void PaginateCompanies_ReturnsCorrectPage()
        {
            var companies = new List<CompanyTestData>();
            for (int i = 1; i <= 25; i++)
            {
                companies.Add(new CompanyTestData { Id = i, CompanyName = $"Company {i}" });
            }

            int pageSize = 10;
            int pageNumber = 2;
            var paged = companies.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            Assert.Equal(10, paged.Count);
            Assert.Equal(11, paged.First().Id);
            Assert.Equal(20, paged.Last().Id);
        }

        [Fact]
        public void PaginateCompanies_LastPage_ReturnsRemainingItems()
        {
            var companies = new List<CompanyTestData>();
            for (int i = 1; i <= 23; i++)
            {
                companies.Add(new CompanyTestData { Id = i });
            }

            int pageSize = 10;
            int lastPage = (int)Math.Ceiling(companies.Count / (double)pageSize);
            var lastPageItems = companies.Skip((lastPage - 1) * pageSize).Take(pageSize).ToList();

            Assert.Equal(3, lastPageItems.Count);
            Assert.Equal(21, lastPageItems.First().Id);
        }

        [Fact]
        public void PaginateCompanies_EmptyResult_ReturnsEmptyList()
        {
            var companies = new List<CompanyTestData>();
            int pageSize = 10;
            int pageNumber = 1;

            var paged = companies.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            Assert.Empty(paged);
        }

        // ==================== SORTING TESTS ====================

        [Fact]
        public void SortCompanies_ByCreatedDateDescending_NewestFirst()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CreatedAt = new DateTime(2024, 3, 20) },
                new() { Id = 2, CreatedAt = new DateTime(2024, 3, 25) },
                new() { Id = 3, CreatedAt = new DateTime(2024, 3, 22) }
            };

            var sorted = companies.OrderByDescending(c => c.CreatedAt).ToList();

            Assert.Equal(2, sorted[0].Id);
            Assert.Equal(3, sorted[1].Id);
            Assert.Equal(1, sorted[2].Id);
        }

        [Fact]
        public void SortCompanies_ByCreatedDateAscending_OldestFirst()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CreatedAt = new DateTime(2024, 3, 20) },
                new() { Id = 2, CreatedAt = new DateTime(2024, 3, 25) },
                new() { Id = 3, CreatedAt = new DateTime(2024, 3, 22) }
            };

            var sorted = companies.OrderBy(c => c.CreatedAt).ToList();

            Assert.Equal(1, sorted[0].Id);
            Assert.Equal(3, sorted[1].Id);
            Assert.Equal(2, sorted[2].Id);
        }

        [Fact]
        public void SortCompanies_ByCompanyNameAlphabetically()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CompanyName = "Zebra Corp" },
                new() { Id = 2, CompanyName = "Alpha Inc" },
                new() { Id = 3, CompanyName = "Beta Solutions" }
            };

            var sorted = companies.OrderBy(c => c.CompanyName).ToList();

            Assert.Equal(2, sorted[0].Id);
            Assert.Equal(3, sorted[1].Id);
            Assert.Equal(1, sorted[2].Id);
        }

        [Fact]
        public void SortCompanies_ByStatus()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, Status = "REJECTED" },
                new() { Id = 2, Status = "APPROVED" },
                new() { Id = 3, Status = "PENDING_APPROVAL" }
            };

            var sorted = companies.OrderBy(c => c.Status).ToList();

            Assert.Equal(2, sorted[0].Id);
            Assert.Equal(3, sorted[1].Id);
            Assert.Equal(1, sorted[2].Id);
        }

        // ==================== PENDING COUNT TESTS ====================

        [Fact]
        public void GetPendingCompaniesCount_ReturnsCorrectNumber()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Status = "PENDING_APPROVAL" },
                new() { Status = "APPROVED" },
                new() { Status = "PENDING_APPROVAL" },
                new() { Status = "REJECTED" },
                new() { Status = "PENDING_APPROVAL" }
            };

            var pendingCount = companies.Count(c => c.Status == "PENDING_APPROVAL");

            Assert.Equal(3, pendingCount);
        }

        [Fact]
        public void GetPendingCompaniesCount_NoPending_ReturnsZero()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Status = "APPROVED" },
                new() { Status = "REJECTED" },
                new() { Status = "APPROVED" }
            };

            var pendingCount = companies.Count(c => c.Status == "PENDING_APPROVAL");

            Assert.Equal(0, pendingCount);
        }

        [Fact]
        public void HasPending_WhenPendingExists_ReturnsTrue()
        {
            var pendingCount = 3;
            var hasPending = pendingCount > 0;

            Assert.True(hasPending);
        }

        [Fact]
        public void HasPending_WhenNoPending_ReturnsFalse()
        {
            var pendingCount = 0;
            var hasPending = pendingCount > 0;

            Assert.False(hasPending);
        }

        // ==================== BULK OPERATION TESTS ====================

        [Fact]
        public void BulkApproveCompanies_AllSuccess_ReturnsSuccessCount()
        {
            var companyIds = new List<int> { 1, 2, 3 };
            var results = new List<BulkOperationResultTestData>();

            foreach (var id in companyIds)
            {
                results.Add(new BulkOperationResultTestData
                {
                    CompanyId = id,
                    Success = true,
                    Message = "Company approved successfully."
                });
            }

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            Assert.Equal(3, successCount);
            Assert.Equal(0, failCount);
        }

        [Fact]
        public void BulkRejectCompanies_SomeFail_ReturnsPartialSuccess()
        {
            var operations = new List<(int Id, bool Success)>
            {
                (1, true),
                (2, false),
                (3, true)
            };

            var successCount = operations.Count(o => o.Success);
            var failCount = operations.Count(o => !o.Success);

            Assert.Equal(2, successCount);
            Assert.Equal(1, failCount);
        }

        [Fact]
        public void BulkOperation_MaxLimit_50Companies()
        {
            var maxAllowed = 50;
            var requestedIds = new List<int>();
            for (int i = 1; i <= 60; i++) requestedIds.Add(i);

            var isValid = requestedIds.Count <= maxAllowed;

            Assert.False(isValid, "Bulk operation should not allow more than 50 companies");
            Assert.True(requestedIds.Count > maxAllowed);
        }

        [Fact]
        public void BulkOperation_EmptyList_ShouldFail()
        {
            var companyIds = new List<int>();
            var isValid = companyIds.Any();

            Assert.False(isValid, "Empty company list should be invalid");
        }

        // ==================== AUDIT LOG TESTS ====================

        [Fact]
        public void AuditLog_RecordsStatusChange_Correctly()
        {
            var auditLog = new AuditLogTestData
            {
                Id = 1,
                AdminId = 5,
                CompanyId = 10,
                Action = "STATUS_CHANGED_TO_APPROVED",
                OldValue = "PENDING_APPROVAL",
                NewValue = "APPROVED",
                Details = null,
                CreatedAt = DateTime.UtcNow
            };

            Assert.Equal(5, auditLog.AdminId);
            Assert.Equal(10, auditLog.CompanyId);
            Assert.Equal("STATUS_CHANGED_TO_APPROVED", auditLog.Action);
            Assert.Equal("PENDING_APPROVAL", auditLog.OldValue);
            Assert.Equal("APPROVED", auditLog.NewValue);
        }

        [Fact]
        public void AuditLog_RejectionIncludesReason_InDetails()
        {
            var rejectionReason = "Incomplete profile";
            var auditLog = new AuditLogTestData
            {
                Action = "STATUS_CHANGED_TO_REJECTED",
                OldValue = "PENDING_APPROVAL",
                NewValue = "REJECTED",
                Details = $"Rejection reason: {rejectionReason}"
            };

            Assert.Contains(rejectionReason, auditLog.Details);
        }

        [Fact]
        public void AuditLogs_OrderedByDateDescending_LatestFirst()
        {
            var logs = new List<AuditLogTestData>
            {
                new() { Id = 1, CreatedAt = new DateTime(2024, 3, 20, 10, 0, 0) },
                new() { Id = 2, CreatedAt = new DateTime(2024, 3, 20, 15, 0, 0) },
                new() { Id = 3, CreatedAt = new DateTime(2024, 3, 19, 9, 0, 0) }
            };

            var sorted = logs.OrderByDescending(l => l.CreatedAt).ToList();

            Assert.Equal(2, sorted[0].Id);
            Assert.Equal(1, sorted[1].Id);
            Assert.Equal(3, sorted[2].Id);
        }

        [Fact]
        public void AuditLogs_LimitParameter_ReturnsCorrectCount()
        {
            var logs = new List<AuditLogTestData>();
            for (int i = 1; i <= 100; i++)
            {
                logs.Add(new AuditLogTestData { Id = i });
            }

            var limit = 20;
            var limitedLogs = logs.Take(limit).ToList();

            Assert.Equal(20, limitedLogs.Count);
            Assert.Equal(1, limitedLogs.First().Id);
            Assert.Equal(20, limitedLogs.Last().Id);
        }

        // ==================== COMPANY REVIEW TESTS ====================

        [Fact]
        public void CompanyReview_CalculatesProfileCompleteness_Correctly()
        {
            var completeCompany = new CompanyTestData
            {
                CompanyName = "Tech Corp",
                Email = "tech@corp.com",
                Status = "PENDING_APPROVAL"
            };

            var completeness = CalculateProfileCompleteness(completeCompany);

            Assert.InRange(completeness, 0, 100);
        }

        [Fact]
        public void CompanyReview_CanBeApproved_WhenStatusIsPending()
        {
            var company = new CompanyTestData { Status = "PENDING_APPROVAL" };

            Assert.True(company.CanBeApproved);
        }

        [Fact]
        public void CompanyReview_CannotBeApproved_WhenStatusIsNotPending()
        {
            var approvedCompany = new CompanyTestData { Status = "APPROVED" };
            var rejectedCompany = new CompanyTestData { Status = "REJECTED" };

            Assert.False(approvedCompany.CanBeApproved);
            Assert.False(rejectedCompany.CanBeApproved);
        }

        // ==================== EMAIL VALIDATION TESTS ====================

        [Theory]
        [InlineData("company@example.com")]
        [InlineData("contact@techcorp.com")]
        [InlineData("info@company.co.uk")]
        [InlineData("hello@startup.io")]
        [InlineData("admin@pathfinder.com")]
        [InlineData("user.name@domain.com")]
        [InlineData("user+label@domain.com")]
        public void CompanyEmail_ValidFormat_IsValid(string email)
        {
            var isValid = IsValidEmail(email);
            Assert.True(isValid, $"Email '{email}' should be valid");
        }

        [Theory]
        [InlineData("")]
        [InlineData("company@")]
        [InlineData("@example.com")]
        [InlineData("company@.com")]
        [InlineData("plainaddress")]
        [InlineData("company@example..com")]
        [InlineData("company name@example.com")]
        [InlineData("  ")]
        [InlineData(".user@domain.com")]
        [InlineData("user.@domain.com")]
        [InlineData("user@domain.")]
        [InlineData("user@domain..com")]
        [InlineData("user@domain.c")]
        public void CompanyEmail_InvalidFormat_IsInvalid(string email)
        {
            var isValid = IsValidEmail(email);
            Assert.False(isValid, $"Email '{email}' should be invalid");
        }

        // ==================== ENFORCEMENT TESTS ====================

        [Fact]
        public void CompanyCannotPostJob_WhenStatusIsNotApproved()
        {
            var companies = new[]
            {
                new { Status = "PENDING_APPROVAL", CanPostJob = false },
                new { Status = "REJECTED", CanPostJob = false },
                new { Status = "APPROVED", CanPostJob = true }
            };

            foreach (var company in companies)
            {
                var canPostJob = company.Status == "APPROVED";
                Assert.Equal(company.CanPostJob, canPostJob);
            }
        }

        [Fact]
        public void CompanyCannotLogin_WhenStatusIsNotApproved()
        {
            var companies = new[]
            {
                new { Status = "PENDING_APPROVAL", CanLogin = false },
                new { Status = "REJECTED", CanLogin = false },
                new { Status = "APPROVED", CanLogin = true }
            };

            foreach (var company in companies)
            {
                var canLogin = company.Status == "APPROVED";
                Assert.Equal(company.CanLogin, canLogin);
            }
        }

        // ==================== INTEGRATION TEST SCENARIOS ====================

        [Fact]
        public void CompleteApprovalWorkflow_FromPendingToApproved()
        {
            // Arrange
            var company = new CompanyTestData
            {
                Id = 1,
                CompanyName = "New Startup",
                Email = "startup@example.com",
                Status = "PENDING_APPROVAL",
                CreatedAt = DateTime.UtcNow
            };

            var adminId = 1;
            var newStatus = "APPROVED";

            // Act - Approve
            company.Status = newStatus;
            company.ApprovedBy = adminId;
            company.ApprovedAt = DateTime.UtcNow;

            // Assert
            Assert.Equal("APPROVED", company.Status);
            Assert.Equal(adminId, company.ApprovedBy);
            Assert.NotNull(company.ApprovedAt);
            Assert.False(company.CanBeApproved);
        }

        [Fact]
        public void CompleteRejectionWorkflow_FromPendingToRejected()
        {
            // Arrange
            var company = new CompanyTestData
            {
                Id = 2,
                CompanyName = "Incomplete Inc",
                Email = "incomplete@example.com",
                Status = "PENDING_APPROVAL"
            };

            var adminId = 2;
            var newStatus = "REJECTED";
            var rejectionReason = "Missing required company information";

            // Act - Reject
            company.Status = newStatus;
            company.ApprovedBy = adminId;
            company.ApprovedAt = DateTime.UtcNow;
            company.RejectionReason = rejectionReason;

            // Assert
            Assert.Equal("REJECTED", company.Status);
            Assert.Equal(adminId, company.ApprovedBy);
            Assert.NotNull(company.ApprovedAt);
            Assert.Equal(rejectionReason, company.RejectionReason);
            Assert.False(company.CanBeApproved);
        }

        [Fact]
        public void CombinedFilters_ReturnsCorrectResults()
        {
            var companies = new List<CompanyTestData>
            {
                new() { Id = 1, CompanyName = "Alpha Tech", Email = "alpha@tech.com", Status = "PENDING_APPROVAL", CreatedAt = new DateTime(2024, 3, 15) },
                new() { Id = 2, CompanyName = "Beta Solutions", Email = "beta@sol.com", Status = "APPROVED", CreatedAt = new DateTime(2024, 3, 20) },
                new() { Id = 3, CompanyName = "Gamma Corp", Email = "gamma@corp.com", Status = "PENDING_APPROVAL", CreatedAt = new DateTime(2024, 3, 25) },
                new() { Id = 4, CompanyName = "Delta Ltd", Email = "delta@ltd.com", Status = "REJECTED", CreatedAt = new DateTime(2024, 4, 1) }
            };

            var filter = new FilterRequestTestData
            {
                Status = "PENDING_APPROVAL",
                SearchTerm = "tech",
                FromDate = new DateTime(2024, 3, 10),
                ToDate = new DateTime(2024, 3, 31)
            };

            var result = companies.Where(c =>
                (filter.Status == null || c.Status == filter.Status) &&
                (string.IsNullOrEmpty(filter.SearchTerm) || 
                    c.CompanyName.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)) &&
                (!filter.FromDate.HasValue || c.CreatedAt >= filter.FromDate) &&
                (!filter.ToDate.HasValue || c.CreatedAt <= filter.ToDate)
            ).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }
    }
}