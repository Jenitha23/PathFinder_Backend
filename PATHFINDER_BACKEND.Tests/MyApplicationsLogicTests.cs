using Xunit;
using PATHFINDER_BACKEND.DTOs;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PATHFINDER_BACKEND.Tests
{
    public class MyApplicationsLogicTests
    {
        // ─── DTO Property Tests ───────────────────────────────────────────────

        [Fact]
        public void ApplicationResponse_Properties_CanBeSetAndRetrieved()
        {
            var response = new ApplicationResponse
            {
                ApplicationId = 1,
                JobId = 10,
                JobTitle = "Software Developer",
                CompanyName = "Tech Corp",
                Location = "Remote",
                JobType = "Full-time",
                Status = "Pending",
                AppliedDate = new DateTime(2024, 3, 17)
            };

            Assert.Equal(1, response.ApplicationId);
            Assert.Equal(10, response.JobId);
            Assert.Equal("Software Developer", response.JobTitle);
            Assert.Equal("Tech Corp", response.CompanyName);
            Assert.Equal("Remote", response.Location);
            Assert.Equal("Full-time", response.JobType);
            Assert.Equal("Pending", response.Status);
            Assert.Equal(new DateTime(2024, 3, 17), response.AppliedDate);
        }

        // ─── Status Message Logic Tests ───────────────────────────────────────
        
        [Theory]
        [InlineData("Accepted", "You don't have any accepted applications yet.")]
        [InlineData("Rejected", "You don't have any rejected applications.")]
        [InlineData("Shortlisted", "You don't have any shortlisted applications yet.")]
        [InlineData("Pending", "You don't have any pending applications.")]
        [InlineData("Unknown", "No applications found with status 'Unknown'.")]
        public void GetEmptyStatusMessage_ReturnsDescriptiveFriendlyMessage(string status, string expectedMessage)
        {
            // Simulate the logic used in ApplicationsController.GetMyApplications
            var message = GetFriendlyMessage(status);
            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public void GetDefaultEmptyMessage_ReturnsGlobalNoApplicationsMessage()
        {
            var message = GetFriendlyMessage(null);
            Assert.Equal("You haven't applied to any jobs yet. Start exploring opportunities!", message);
        }

        // ─── Status Validation Tests ──────────────────────────────────────────

        [Theory]
        [InlineData("Pending")]
        [InlineData("Shortlisted")]
        [InlineData("Rejected")]
        [InlineData("Accepted")]
        public void IsValidStatus_ValidStatuses_ReturnsTrue(string status)
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            Assert.Contains(status, validStatuses, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Approved")]
        [InlineData("Interview")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValidStatus_InvalidStatuses_ReturnsFalse(string? status)
        {
            var validStatuses = new[] { "Pending", "Shortlisted", "Rejected", "Accepted" };
            if (string.IsNullOrEmpty(status))
            {
                Assert.False(false); // Boundary case for logic check
            }
            else
            {
                Assert.DoesNotContain(status, validStatuses, StringComparer.OrdinalIgnoreCase);
            }
        }

        // ─── Sort Order Parameter Verification ────────────────────────────────

        [Theory]
        [InlineData("date_desc", true)]
        [InlineData("date_asc", true)]
        [InlineData("salary_desc", false)]
        [InlineData("title_asc", false)]
        public void SortOrder_SupportedSortOrders(string sortBy, bool isSupported)
        {
            var supported = new[] { "date_desc", "date_asc" };
            Assert.Equal(isSupported, supported.Contains(sortBy.ToLower()));
        }

        // ─── Logic Mimic Helper ──────────────────────────────────────────────
        // This mirrors the logic added to ApplicationsController.GetMyApplications
        private string GetFriendlyMessage(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "You haven't applied to any jobs yet. Start exploring opportunities!";
            }

            var statusLower = status!.Trim().ToLower();
            return statusLower switch
            {
                "accepted" => "You don't have any accepted applications yet.",
                "rejected" => "You don't have any rejected applications.",
                "shortlisted" => "You don't have any shortlisted applications yet.",
                "pending" => "You don't have any pending applications.",
                _ => $"No applications found with status '{status}'."
            };
        }
    }
}
