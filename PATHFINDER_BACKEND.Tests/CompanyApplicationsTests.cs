using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PATHFINDER_BACKEND.Tests
{
    public class CompanyApplicationsTests
    {
        // ─── Application Status Validation Tests ─────────────────────────────────

        [Fact]
        public void UpdateApplicationStatus_ValidStatus_IsValid()
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            
            foreach (var status in validStatuses)
            {
                var isValid = IsValidApplicationStatus(status);
                Assert.True(isValid, $"Status '{status}' should be valid");
            }
        }

        [Fact]
        public void UpdateApplicationStatus_InvalidStatus_IsInvalid()
        {
            var invalidStatuses = new[] { "", "Approved", "Declined", "InReview", "Hired", "  " };
            
            foreach (var status in invalidStatuses)
            {
                var isValid = IsValidApplicationStatus(status);
                Assert.False(isValid, $"Status '{status}' should be invalid");
            }
        }

        [Fact]
        public void UpdateApplicationStatus_ValidationErrors_ContainsCorrectMessages()
        {
            var errors = new List<string>();
            
            // Test empty status
            var emptyStatus = "";
            if (string.IsNullOrWhiteSpace(emptyStatus))
                errors.Add("Status is required.");
            
            // Test invalid status
            var invalidStatus = "InvalidStatus";
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            if (!validStatuses.Contains(invalidStatus))
                errors.Add("Status must be one of: Pending, Shortlisted, Rejected, Accepted");
            
            // Test valid status
            var validStatus = "Shortlisted";
            if (string.IsNullOrWhiteSpace(validStatus))
                errors.Add("Status is required.");
            else if (!validStatuses.Contains(validStatus))
                errors.Add("Status must be one of: Pending, Shortlisted, Rejected, Accepted");
            
            Assert.Equal(2, errors.Count);
            Assert.Contains("Status is required.", errors);
            Assert.Contains("Status must be one of: Pending, Shortlisted, Rejected, Accepted", errors);
        }

        [Fact]
        public void UpdateApplicationStatus_CaseInsensitive_ValidStatusAccepted()
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            var testStatuses = new[] { "pending", "SHORTLISTED", "reJECTED", "ACCEPTED" };
            
            foreach (var status in testStatuses)
            {
                var isValid = validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
                Assert.True(isValid, $"Case-insensitive status '{status}' should be valid");
            }
        }

        // ─── Application Filter Tests ───────────────────────────────────────────

        [Fact]
        public void FilterApplications_ByStatus_ReturnsCorrectCount()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, Status = "Pending" },
                new() { ApplicationId = 2, Status = "Shortlisted" },
                new() { ApplicationId = 3, Status = "Pending" },
                new() { ApplicationId = 4, Status = "Rejected" },
                new() { ApplicationId = 5, Status = "Accepted" },
                new() { ApplicationId = 6, Status = "Shortlisted" }
            };
            
            var pendingApps = applications.Where(a => a.Status == "Pending").ToList();
            var shortlistedApps = applications.Where(a => a.Status == "Shortlisted").ToList();
            var rejectedApps = applications.Where(a => a.Status == "Rejected").ToList();
            var acceptedApps = applications.Where(a => a.Status == "Accepted").ToList();
            
            Assert.Equal(2, pendingApps.Count);
            Assert.Equal(2, shortlistedApps.Count);
            Assert.Equal(1, rejectedApps.Count);
            Assert.Equal(1, acceptedApps.Count);
        }

        [Fact]
        public void FilterApplications_ByJobId_ReturnsCorrectCount()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, JobId = 1, StudentName = "John" },
                new() { ApplicationId = 2, JobId = 1, StudentName = "Jane" },
                new() { ApplicationId = 3, JobId = 2, StudentName = "Bob" },
                new() { ApplicationId = 4, JobId = 2, StudentName = "Alice" },
                new() { ApplicationId = 5, JobId = 2, StudentName = "Charlie" },
                new() { ApplicationId = 6, JobId = 3, StudentName = "Diana" }
            };
            
            var job1Apps = applications.Where(a => a.JobId == 1).ToList();
            var job2Apps = applications.Where(a => a.JobId == 2).ToList();
            var job3Apps = applications.Where(a => a.JobId == 3).ToList();
            
            Assert.Equal(2, job1Apps.Count);
            Assert.Equal(3, job2Apps.Count);
            Assert.Equal(1, job3Apps.Count);
        }

        [Fact]
        public void FilterApplications_ByMultipleCriteria_ReturnsCorrectResults()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, JobId = 1, Status = "Pending", StudentName = "John" },
                new() { ApplicationId = 2, JobId = 1, Status = "Shortlisted", StudentName = "Jane" },
                new() { ApplicationId = 3, JobId = 2, Status = "Pending", StudentName = "Bob" },
                new() { ApplicationId = 4, JobId = 2, Status = "Rejected", StudentName = "Alice" },
                new() { ApplicationId = 5, JobId = 1, Status = "Pending", StudentName = "Charlie" }
            };
            
            var filtered = applications
                .Where(a => a.JobId == 1 && a.Status == "Pending")
                .ToList();
            
            Assert.Equal(2, filtered.Count);
            Assert.Contains(filtered, a => a.StudentName == "John");
            Assert.Contains(filtered, a => a.StudentName == "Charlie");
            Assert.DoesNotContain(filtered, a => a.StudentName == "Jane");
        }

        // ─── Application Sorting Tests ──────────────────────────────────────────

        [Fact]
        public void SortApplications_ByDateDescending_LatestFirst()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, AppliedDate = new DateTime(2024, 3, 20, 10, 0, 0) },
                new() { ApplicationId = 2, AppliedDate = new DateTime(2024, 3, 22, 15, 30, 0) },
                new() { ApplicationId = 3, AppliedDate = new DateTime(2024, 3, 21, 9, 15, 0) },
                new() { ApplicationId = 4, AppliedDate = new DateTime(2024, 3, 19, 14, 45, 0) }
            };
            
            var sorted = applications.OrderByDescending(a => a.AppliedDate).ToList();
            
            Assert.Equal(2, sorted[0].ApplicationId);
            Assert.Equal(3, sorted[1].ApplicationId);
            Assert.Equal(1, sorted[2].ApplicationId);
            Assert.Equal(4, sorted[3].ApplicationId);
        }

        [Fact]
        public void SortApplications_ByDateAscending_OldestFirst()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, AppliedDate = new DateTime(2024, 3, 20, 10, 0, 0) },
                new() { ApplicationId = 2, AppliedDate = new DateTime(2024, 3, 22, 15, 30, 0) },
                new() { ApplicationId = 3, AppliedDate = new DateTime(2024, 3, 21, 9, 15, 0) },
                new() { ApplicationId = 4, AppliedDate = new DateTime(2024, 3, 19, 14, 45, 0) }
            };
            
            var sorted = applications.OrderBy(a => a.AppliedDate).ToList();
            
            Assert.Equal(4, sorted[0].ApplicationId);
            Assert.Equal(1, sorted[1].ApplicationId);
            Assert.Equal(3, sorted[2].ApplicationId);
            Assert.Equal(2, sorted[3].ApplicationId);
        }

        // ─── Company Ownership Validation Tests ─────────────────────────────────

        [Fact]
        public void VerifyCompanyOwnsJob_ValidOwnership_ReturnsTrue()
        {
            var jobs = new List<JobOwnershipTestData>
            {
                new() { JobId = 1, CompanyId = 10 },
                new() { JobId = 2, CompanyId = 10 },
                new() { JobId = 3, CompanyId = 20 }
            };
            
            var companyId = 10;
            var jobId = 1;
            
            var ownsJob = jobs.Any(j => j.JobId == jobId && j.CompanyId == companyId);
            
            Assert.True(ownsJob);
        }

        [Fact]
        public void VerifyCompanyOwnsJob_InvalidOwnership_ReturnsFalse()
        {
            var jobs = new List<JobOwnershipTestData>
            {
                new() { JobId = 1, CompanyId = 10 },
                new() { JobId = 2, CompanyId = 10 },
                new() { JobId = 3, CompanyId = 20 }
            };
            
            var companyId = 30;
            var jobId = 1;
            
            var ownsJob = jobs.Any(j => j.JobId == jobId && j.CompanyId == companyId);
            
            Assert.False(ownsJob);
        }

        [Fact]
        public void VerifyApplicationBelongsToCompany_ValidOwnership_ReturnsTrue()
        {
            var applications = new List<ApplicationOwnershipTestData>
            {
                new() { ApplicationId = 1, JobId = 1, CompanyId = 10 },
                new() { ApplicationId = 2, JobId = 2, CompanyId = 10 },
                new() { ApplicationId = 3, JobId = 3, CompanyId = 20 }
            };
            
            var companyId = 10;
            var applicationId = 1;
            
            var ownsApplication = applications.Any(a => a.ApplicationId == applicationId && a.CompanyId == companyId);
            
            Assert.True(ownsApplication);
        }

        [Fact]
        public void VerifyApplicationBelongsToCompany_InvalidOwnership_ReturnsFalse()
        {
            var applications = new List<ApplicationOwnershipTestData>
            {
                new() { ApplicationId = 1, JobId = 1, CompanyId = 10 },
                new() { ApplicationId = 2, JobId = 2, CompanyId = 10 },
                new() { ApplicationId = 3, JobId = 3, CompanyId = 20 }
            };
            
            var companyId = 10;
            var applicationId = 3; // This belongs to company 20
            
            var ownsApplication = applications.Any(a => a.ApplicationId == applicationId && a.CompanyId == companyId);
            
            Assert.False(ownsApplication);
        }

        // ─── Application Statistics Tests ───────────────────────────────────────

        [Fact]
        public void GetApplicationStats_CalculatesCorrectCounts()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { Status = "Pending" },
                new() { Status = "Pending" },
                new() { Status = "Shortlisted" },
                new() { Status = "Rejected" },
                new() { Status = "Accepted" },
                new() { Status = "Shortlisted" },
                new() { Status = "Pending" }
            };
            
            var stats = new
            {
                Total = applications.Count,
                Pending = applications.Count(a => a.Status == "Pending"),
                Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                Rejected = applications.Count(a => a.Status == "Rejected"),
                Accepted = applications.Count(a => a.Status == "Accepted")
            };
            
            Assert.Equal(7, stats.Total);
            Assert.Equal(3, stats.Pending);
            Assert.Equal(2, stats.Shortlisted);
            Assert.Equal(1, stats.Rejected);
            Assert.Equal(1, stats.Accepted);
        }

        [Fact]
        public void GetApplicationStats_EmptyList_ReturnsZeroCounts()
        {
            var applications = new List<ApplicationTestData>();
            
            var stats = new
            {
                Total = applications.Count,
                Pending = applications.Count(a => a.Status == "Pending"),
                Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                Rejected = applications.Count(a => a.Status == "Rejected"),
                Accepted = applications.Count(a => a.Status == "Accepted")
            };
            
            Assert.Equal(0, stats.Total);
            Assert.Equal(0, stats.Pending);
            Assert.Equal(0, stats.Shortlisted);
            Assert.Equal(0, stats.Rejected);
            Assert.Equal(0, stats.Accepted);
        }

        [Fact]
        public void GetUniqueStudentsCount_ReturnsCorrectNumber()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { StudentId = 1, StudentName = "John" },
                new() { StudentId = 1, StudentName = "John" }, // Duplicate
                new() { StudentId = 2, StudentName = "Jane" },
                new() { StudentId = 3, StudentName = "Bob" },
                new() { StudentId = 3, StudentName = "Bob" }  // Duplicate
            };
            
            var uniqueStudents = applications.Select(a => a.StudentId).Distinct().Count();
            
            Assert.Equal(3, uniqueStudents);
        }

        [Fact]
        public void GetJobsWithApplications_ReturnsCorrectCount()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { JobId = 1 },
                new() { JobId = 1 },
                new() { JobId = 2 },
                new() { JobId = 3 },
                new() { JobId = 3 },
                new() { JobId = 3 }
            };
            
            var jobsWithApps = applications.Select(a => a.JobId).Distinct().Count();
            
            Assert.Equal(3, jobsWithApps);
        }

        [Fact]
        public void GetMostAppliedJob_ReturnsCorrectJob()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { JobId = 1, JobTitle = "Frontend Developer" },
                new() { JobId = 1, JobTitle = "Frontend Developer" },
                new() { JobId = 2, JobTitle = "Backend Developer" },
                new() { JobId = 1, JobTitle = "Frontend Developer" },
                new() { JobId = 3, JobTitle = "Full Stack Developer" },
                new() { JobId = 2, JobTitle = "Backend Developer" }
            };
            
            var mostAppliedJob = applications
                .GroupBy(a => a.JobTitle)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
            
            Assert.Equal("Frontend Developer", mostAppliedJob);
        }

        // ─── Applicant Data Validation Tests ────────────────────────────────────

        [Fact]
        public void ApplicantResponse_ContainsRequiredFields()
        {
            var applicant = new ApplicantResponseTestData
            {
                ApplicationId = 1,
                StudentId = 100,
                StudentName = "John Doe",
                StudentEmail = "john@example.com",
                Status = "Pending",
                AppliedDate = DateTime.UtcNow,
                JobTitle = "Software Engineer",
                CvUrl = "https://example.com/cv.pdf"
            };
            
            Assert.NotNull(applicant.StudentName);
            Assert.NotNull(applicant.StudentEmail);
            Assert.Contains("@", applicant.StudentEmail);
            Assert.NotNull(applicant.Status);
            Assert.True(applicant.ApplicationId > 0);
            Assert.True(applicant.StudentId > 0);
        }

        [Fact]
        public void ApplicantResponse_EmailFormat_ValidEmailsPass()
        {
            var validEmails = new[] { "john@example.com", "jane.doe@company.co.uk", "test@test.com" };
            var invalidEmails = new[] { "invalid", "missing@at", "no-dot@com", "" };
            
            foreach (var email in validEmails)
            {
                var isValid = IsValidEmail(email);
                Assert.True(isValid, $"Email '{email}' should be valid");
            }
            
            foreach (var email in invalidEmails)
            {
                var isValid = IsValidEmail(email);
                Assert.False(isValid, $"Email '{email}' should be invalid");
            }
        }

        [Fact]
        public void ApplicantResponse_CvUrl_ValidUrlsAccepted()
        {
            var validUrls = new[] 
            { 
                "https://azurestorage.blob.core.windows.net/cv.pdf",
                "https://example.com/uploads/resume.docx",
                "http://localhost:5249/uploads/cv.pdf"
            };
            
            var invalidUrls = new[] { "", "not-a-url", "ftp://invalid.com" };
            
            foreach (var url in validUrls)
            {
                var isValid = Uri.IsWellFormedUriString(url, UriKind.Absolute);
                Assert.True(isValid, $"URL '{url}' should be valid");
            }
            
            foreach (var url in invalidUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var isValid = Uri.IsWellFormedUriString(url, UriKind.Absolute);
                    Assert.False(isValid, $"URL '{url}' should be invalid");
                }
            }
        }

        // ─── Pagination Tests (if implemented) ──────────────────────────────────

        [Fact]
        public void PaginateApplications_ReturnsCorrectPage()
        {
            var applications = new List<ApplicationTestData>();
            for (int i = 1; i <= 25; i++)
            {
                applications.Add(new ApplicationTestData { ApplicationId = i });
            }
            
            int pageSize = 10;
            int pageNumber = 2;
            
            var pagedApplications = applications
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            Assert.Equal(10, pagedApplications.Count);
            Assert.Equal(11, pagedApplications.First().ApplicationId);
            Assert.Equal(20, pagedApplications.Last().ApplicationId);
        }

        [Fact]
        public void PaginateApplications_LastPage_ReturnsRemainingItems()
        {
            var applications = new List<ApplicationTestData>();
            for (int i = 1; i <= 23; i++)
            {
                applications.Add(new ApplicationTestData { ApplicationId = i });
            }
            
            int pageSize = 10;
            int pageNumber = 3;
            
            var pagedApplications = applications
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            Assert.Equal(3, pagedApplications.Count);
            Assert.Equal(21, pagedApplications.First().ApplicationId);
            Assert.Equal(23, pagedApplications.Last().ApplicationId);
        }

        // ─── Helper Methods ─────────────────────────────────────────────────────

        private static bool IsValidApplicationStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            return validStatuses.Contains(status);
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ─── Test Data Classes ──────────────────────────────────────────────────

        public class ApplicationTestData
        {
            public int ApplicationId { get; set; }
            public int StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string StudentEmail { get; set; } = "";
            public int JobId { get; set; }
            public string JobTitle { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime AppliedDate { get; set; }
            public string? CvUrl { get; set; }
            public string? CoverLetter { get; set; }
            public string? Skills { get; set; }
            public string? University { get; set; }
            public string? Degree { get; set; }
        }

        private class JobOwnershipTestData
        {
            public int JobId { get; set; }
            public int CompanyId { get; set; }
        }

        private class ApplicationOwnershipTestData
        {
            public int ApplicationId { get; set; }
            public int JobId { get; set; }
            public int CompanyId { get; set; }
        }

        private class ApplicantResponseTestData
        {
            public int ApplicationId { get; set; }
            public int StudentId { get; set; }
            public string StudentName { get; set; } = "";
            public string StudentEmail { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime AppliedDate { get; set; }
            public string JobTitle { get; set; } = "";
            public string? CvUrl { get; set; }
        }
    }

    // ─── Company Job Update Tests (Extended) ─────────────────────────────────────

    public class CompanyJobUpdateDeleteExtendedTests
    {
        [Fact]
        public void UpdateJob_ValidData_IsValid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "Senior Software Engineer",
                Description = "Updated description with new responsibilities",
                Requirements = "7+ years experience, C#, .NET Core",
                Location = "Colombo, Sri Lanka",
                JobType = "Full-time",
                Category = "Technology",
                ApplicationDeadline = DateTime.UtcNow.AddDays(30)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.True(isValid);
        }

        [Fact]
        public void UpdateJob_EmptyTitle_IsInvalid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "",
                Description = "Valid description",
                Requirements = "Valid requirements",
                Location = "Colombo",
                JobType = "Full-time",
                Category = "Technology",
                ApplicationDeadline = DateTime.UtcNow.AddDays(30)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.False(isValid);
        }

        [Fact]
        public void UpdateJob_EmptyDescription_IsInvalid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "Valid Title",
                Description = "",
                Requirements = "Valid requirements",
                Location = "Colombo",
                JobType = "Full-time",
                Category = "Technology",
                ApplicationDeadline = DateTime.UtcNow.AddDays(30)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.False(isValid);
        }

        [Fact]
        public void UpdateJob_PastDeadline_IsInvalid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "Valid Title",
                Description = "Valid description",
                Requirements = "Valid requirements",
                Location = "Colombo",
                JobType = "Full-time",
                Category = "Technology",
                ApplicationDeadline = DateTime.UtcNow.AddDays(-1)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.False(isValid);
        }

        [Fact]
        public void UpdateJob_InvalidJobType_IsInvalid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "Valid Title",
                Description = "Valid description",
                Requirements = "Valid requirements",
                Location = "Colombo",
                JobType = "InvalidType",
                Category = "Technology",
                ApplicationDeadline = DateTime.UtcNow.AddDays(30)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.False(isValid);
        }

        [Fact]
        public void UpdateJob_InvalidCategory_IsInvalid()
        {
            var updateRequest = new UpdateJobRequestTest
            {
                Title = "Valid Title",
                Description = "Valid description",
                Requirements = "Valid requirements",
                Location = "Colombo",
                JobType = "Full-time",
                Category = "InvalidCategory",
                ApplicationDeadline = DateTime.UtcNow.AddDays(30)
            };

            var isValid = ValidateUpdateRequest(updateRequest);
            Assert.False(isValid);
        }

        [Fact]
        public void UpdateJob_ValidationErrors_ContainsCorrectMessages()
        {
            var errors = new List<string>();

            var updateRequest = new UpdateJobRequestTest
            {
                Title = "",
                Description = "",
                Requirements = "",
                Location = "",
                JobType = "",
                Category = "",
                ApplicationDeadline = DateTime.UtcNow.AddDays(-1)
            };

            if (string.IsNullOrWhiteSpace(updateRequest.Title))
                errors.Add("Job title is required.");
            
            if (string.IsNullOrWhiteSpace(updateRequest.Description))
                errors.Add("Job description is required.");
            
            if (string.IsNullOrWhiteSpace(updateRequest.Requirements))
                errors.Add("Requirements are required.");
            
            if (string.IsNullOrWhiteSpace(updateRequest.Location))
                errors.Add("Location is required.");
            
            if (string.IsNullOrWhiteSpace(updateRequest.JobType))
                errors.Add("Job type is required.");
            
            if (string.IsNullOrWhiteSpace(updateRequest.Category))
                errors.Add("Category is required.");
            
            if (updateRequest.ApplicationDeadline.Date <= DateTime.UtcNow.Date)
                errors.Add("Job deadline must be a future date.");

            Assert.Contains("Job title is required.", errors);
            Assert.Contains("Job description is required.", errors);
            Assert.Contains("Requirements are required.", errors);
            Assert.Contains("Location is required.", errors);
            Assert.Contains("Job type is required.", errors);
            Assert.Contains("Category is required.", errors);
            Assert.Contains("Job deadline must be a future date.", errors);
            Assert.Equal(7, errors.Count);
        }

        [Fact]
        public void SoftDeleteJob_MarksIsDeleted_JobNotVisible()
        {
            var job = new JobSoftDeleteTestData { JobId = 1, IsDeleted = false, Title = "Test Job" };
            
            // Perform soft delete
            job.IsDeleted = true;
            
            Assert.True(job.IsDeleted);
        }

        [Fact]
        public void SoftDeleteJob_WithExistingApplications_StillCanSoftDelete()
        {
            var job = new JobSoftDeleteTestData { JobId = 1, IsDeleted = false, Title = "Test Job" };
            var applications = new List<int> { 1, 2, 3 }; // Application IDs
            
            // Soft delete the job (should be allowed even with applications)
            job.IsDeleted = true;
            
            Assert.True(job.IsDeleted);
            Assert.Equal(3, applications.Count); // Applications still exist
        }

        [Fact]
        public void HardDeleteJob_WithoutApplications_CanBeDeleted()
        {
            var jobId = 1;
            var applications = new List<int>(); // No applications
            
            var canHardDelete = !applications.Any();
            
            Assert.True(canHardDelete);
        }

        [Fact]
        public void HardDeleteJob_WithApplications_CannotBeDeleted()
        {
            var jobId = 1;
            var applications = new List<int> { 1, 2 }; // Has applications
            
            var canHardDelete = !applications.Any();
            
            Assert.False(canHardDelete);
        }

        [Fact]
        public void BatchSoftDelete_DeleteMultipleJobs_AllJobsArchived()
        {
            var jobs = new List<JobSoftDeleteTestData>
            {
                new() { JobId = 1, IsDeleted = false, Title = "Job 1" },
                new() { JobId = 2, IsDeleted = false, Title = "Job 2" },
                new() { JobId = 3, IsDeleted = false, Title = "Job 3" }
            };

            var jobsToDelete = jobs.Where(j => j.JobId == 1 || j.JobId == 3).ToList();

            foreach (var job in jobsToDelete)
            {
                job.IsDeleted = true;
            }

            var activeJobs = jobs.Where(j => !j.IsDeleted).ToList();

            Assert.Single(activeJobs);
            Assert.Equal(2, activeJobs[0].JobId);
        }

        // ─── Helper Methods ─────────────────────────────────────────────────────

        private static bool ValidateUpdateRequest(UpdateJobRequestTest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return false;
            if (request.Title.Length > 200) return false;
            if (string.IsNullOrWhiteSpace(request.Description)) return false;
            if (string.IsNullOrWhiteSpace(request.Requirements)) return false;
            if (string.IsNullOrWhiteSpace(request.Location)) return false;
            if (request.Location.Length > 150) return false;
            if (!IsValidJobType(request.JobType)) return false;
            if (!IsValidCategory(request.Category)) return false;
            if (request.ApplicationDeadline.Date <= DateTime.UtcNow.Date) return false;
            
            return true;
        }

        private static bool IsValidJobType(string? jobType)
        {
            if (string.IsNullOrWhiteSpace(jobType)) return false;
            if (jobType.Length > 50) return false;
            
            var validTypes = new[] { "Full-time", "Part-time", "Contract", "Internship", "Remote", "Hybrid", "Freelance" };
            return validTypes.Contains(jobType);
        }

        private static bool IsValidCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;
            if (category.Length > 100) return false;
            
            var validCategories = new[] 
            { 
                "Technology", "Marketing", "Sales", "Finance", "Human Resources", 
                "Operations", "Customer Service", "Design", "Healthcare", "Education", 
                "Engineering", "Consulting", "Legal", "Administrative" 
            };
            return validCategories.Contains(category);
        }

        // ─── Test Data Classes ─────────────────────────────────────────────────

        public class UpdateJobRequestTest
        {
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Requirements { get; set; } = "";
            public string? Responsibilities { get; set; }
            public string? Salary { get; set; }
            public string? SalaryRange { get; set; }
            public string Location { get; set; } = "";
            public string JobType { get; set; } = "";
            public string Category { get; set; } = "";
            public string? ExperienceLevel { get; set; }
            public DateTime ApplicationDeadline { get; set; }
        }

        private class JobSoftDeleteTestData
        {
            public int JobId { get; set; }
            public bool IsDeleted { get; set; }
            public string Title { get; set; } = "";
        }
    }
}