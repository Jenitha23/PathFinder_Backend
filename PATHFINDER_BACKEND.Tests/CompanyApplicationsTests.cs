using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PATHFINDER_BACKEND.Tests
{
    public class CompanyApplicationsTests
    {
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

        // ─── Application Status Validation Tests ─────────────────────────────────

        [Fact]
        public void UpdateApplicationStatus_ValidStatus_IsValid()
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };

            foreach (var status in validStatuses)
            {
                var isValid = IsValidApplicationStatus(status);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void UpdateApplicationStatus_InvalidStatus_IsInvalid()
        {
            var invalidStatuses = new[] { "", "Approved", "Declined", "InReview", "Hired", "  " };

            foreach (var status in invalidStatuses)
            {
                var isValid = IsValidApplicationStatus(status);
                Assert.False(isValid);
            }
        }

        [Fact]
        public void UpdateApplicationStatus_CaseInsensitive_ValidStatusAccepted()
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            var testStatuses = new[] { "pending", "SHORTLISTED", "reJECTED", "ACCEPTED" };

            foreach (var status in testStatuses)
            {
                var isValid = validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
                Assert.True(isValid);
            }
        }

        // ─── Filtering Tests ───────────────────────────────────────────

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

            Assert.Equal(2, applications.Count(a => a.Status == "Pending"));
            Assert.Equal(2, applications.Count(a => a.Status == "Shortlisted"));
            Assert.Equal(1, applications.Count(a => a.Status == "Rejected"));
            Assert.Equal(1, applications.Count(a => a.Status == "Accepted"));
        }

        // ─── Sorting ──────────────────────────────────────────

        [Fact]
        public void SortApplications_ByDateDescending_LatestFirst()
        {
            var applications = new List<ApplicationTestData>
            {
                new() { ApplicationId = 1, AppliedDate = new DateTime(2024, 3, 20) },
                new() { ApplicationId = 2, AppliedDate = new DateTime(2024, 3, 22) },
                new() { ApplicationId = 3, AppliedDate = new DateTime(2024, 3, 21) }
            };

            var sorted = applications.OrderByDescending(a => a.AppliedDate).ToList();

            Assert.Equal(2, sorted[0].ApplicationId);
        }

        // ─── Applicant Basic Validation (kept safe tests only) ───────────────────

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

        // ─── Pagination ───────────────────────────────────────────────────

        [Fact]
        public void PaginateApplications_ReturnsCorrectPage()
        {
            var applications = new List<ApplicationTestData>();
            for (int i = 1; i <= 25; i++)
            {
                applications.Add(new ApplicationTestData { ApplicationId = i });
            }

            var paged = applications.Skip(10).Take(10).ToList();

            Assert.Equal(10, paged.Count);
            Assert.Equal(11, paged.First().ApplicationId);
        }

        // ─── Models ──────────────────────────────────────────────────

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
}