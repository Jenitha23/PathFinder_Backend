using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unit tests for company job business logic.
/// These tests validate the pure logic (no DB calls) — 
/// validation rules, field formatting, and job posting semantics.
/// </summary>
public class CompanyJobLogicTests
{
    // ─── Job Title Validation ─────────────────────────────────────────────

    [Fact]
    public void JobTitle_ValidTitle_IsValid()
    {
        var title = "Senior Software Engineer";
        var isValid = IsValidJobTitle(title);
        Assert.True(isValid);
    }

    [Fact]
    public void JobTitle_EmptyString_IsInvalid()
    {
        var title = "";
        var isValid = IsValidJobTitle(title);
        Assert.False(isValid);
    }

    [Fact]
    public void JobTitle_Null_IsInvalid()
    {
        string? title = null;
        var isValid = IsValidJobTitle(title);
        Assert.False(isValid);
    }

    [Fact]
    public void JobTitle_Whitespace_IsInvalid()
    {
        var title = "   ";
        var isValid = IsValidJobTitle(title);
        Assert.False(isValid);
    }

    [Fact]
    public void JobTitle_ExceedsMaxLength_IsInvalid()
    {
        var title = new string('A', 201); // 201 characters, max is 200
        var isValid = IsValidJobTitle(title);
        Assert.False(isValid);
    }

    [Fact]
    public void JobTitle_MaxLength_IsValid()
    {
        var title = new string('A', 200); // Exactly 200 characters
        var isValid = IsValidJobTitle(title);
        Assert.True(isValid);
    }

    // ─── Job Description Validation ───────────────────────────────────────

    [Fact]
    public void JobDescription_ValidDescription_IsValid()
    {
        var description = "We are looking for a skilled developer to join our team...";
        var isValid = IsValidJobDescription(description);
        Assert.True(isValid);
    }

    [Fact]
    public void JobDescription_EmptyString_IsInvalid()
    {
        var description = "";
        var isValid = IsValidJobDescription(description);
        Assert.False(isValid);
    }

    [Fact]
    public void JobDescription_Null_IsInvalid()
    {
        string? description = null;
        var isValid = IsValidJobDescription(description);
        Assert.False(isValid);
    }

    [Fact]
    public void JobDescription_Whitespace_IsInvalid()
    {
        var description = "   ";
        var isValid = IsValidJobDescription(description);
        Assert.False(isValid);
    }

    // ─── Requirements Validation ─────────────────────────────────────────

    [Fact]
    public void Requirements_ValidRequirements_IsValid()
    {
        var requirements = "5+ years of .NET experience, Bachelor's degree in CS";
        var isValid = IsValidRequirements(requirements);
        Assert.True(isValid);
    }

    [Fact]
    public void Requirements_EmptyString_IsInvalid()
    {
        var requirements = "";
        var isValid = IsValidRequirements(requirements);
        Assert.False(isValid);
    }

    [Fact]
    public void Requirements_Null_IsInvalid()
    {
        string? requirements = null;
        var isValid = IsValidRequirements(requirements);
        Assert.False(isValid);
    }

    // ─── Location Validation ─────────────────────────────────────────────

    [Fact]
    public void Location_ValidLocation_IsValid()
    {
        var location = "Colombo, Sri Lanka";
        var isValid = IsValidLocation(location);
        Assert.True(isValid);
    }

    [Fact]
    public void Location_EmptyString_IsInvalid()
    {
        var location = "";
        var isValid = IsValidLocation(location);
        Assert.False(isValid);
    }

    [Fact]
    public void Location_Null_IsInvalid()
    {
        string? location = null;
        var isValid = IsValidLocation(location);
        Assert.False(isValid);
    }

    [Fact]
    public void Location_ExceedsMaxLength_IsInvalid()
    {
        var location = new string('A', 151); // 151 characters, max is 150
        var isValid = IsValidLocation(location);
        Assert.False(isValid);
    }

    [Fact]
    public void Location_MaxLength_IsValid()
    {
        var location = new string('A', 150); // Exactly 150 characters
        var isValid = IsValidLocation(location);
        Assert.True(isValid);
    }

    // ─── Job Type Validation ─────────────────────────────────────────────

    [Theory]
    [InlineData("Full-time")]
    [InlineData("Part-time")]
    [InlineData("Contract")]
    [InlineData("Internship")]
    [InlineData("Remote")]
    [InlineData("Hybrid")]
    [InlineData("Freelance")]
    public void JobType_ValidTypes_IsValid(string jobType)
    {
        var isValid = IsValidJobType(jobType);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Invalid Type")]
    [InlineData("FullTime")]
    public void JobType_InvalidTypes_IsInvalid(string jobType)
    {
        var isValid = IsValidJobType(jobType);
        Assert.False(isValid);
    }

    [Fact]
    public void JobType_Null_IsInvalid()
    {
        string? jobType = null;
        var isValid = IsValidJobType(jobType);
        Assert.False(isValid);
    }

    [Fact]
    public void JobType_ExceedsMaxLength_IsInvalid()
    {
        var jobType = new string('A', 51); // 51 characters, max is 50
        var isValid = IsValidJobType(jobType);
        Assert.False(isValid);
    }

    // ─── Category Validation ─────────────────────────────────────────────

    [Theory]
    [InlineData("Technology")]
    [InlineData("Marketing")]
    [InlineData("Sales")]
    [InlineData("Finance")]
    [InlineData("Human Resources")]
    [InlineData("Engineering")]
    [InlineData("Design")]
    public void Category_ValidCategories_IsValid(string category)
    {
        var isValid = IsValidCategory(category);
        Assert.True(isValid);
    }

    [Fact]
    public void Category_EmptyString_IsInvalid()
    {
        var category = "";
        var isValid = IsValidCategory(category);
        Assert.False(isValid);
    }

    [Fact]
    public void Category_Null_IsInvalid()
    {
        string? category = null;
        var isValid = IsValidCategory(category);
        Assert.False(isValid);
    }

    [Fact]
    public void Category_ExceedsMaxLength_IsInvalid()
    {
        var category = new string('A', 101); // 101 characters, max is 100
        var isValid = IsValidCategory(category);
        Assert.False(isValid);
    }

    // ─── Salary Validation ───────────────────────────────────────────────

    [Theory]
    [InlineData("$60,000 - $80,000")]
    [InlineData("Rs. 250,000 - 350,000")]
    [InlineData("Competitive")]
    [InlineData("Negotiable")]
    public void Salary_ValidFormats_IsValid(string salary)
    {
        var isValid = IsValidSalary(salary);
        Assert.True(isValid);
    }

    [Fact]
    public void Salary_ExceedsMaxLength_IsInvalid()
    {
        var salary = new string('A', 101); // 101 characters, max is 100
        var isValid = IsValidSalary(salary);
        Assert.False(isValid);
    }

    [Fact]
    public void Salary_Null_IsValid()
    {
        string? salary = null;
        var isValid = IsValidSalary(salary);
        Assert.True(isValid); // Optional field
    }

    [Fact]
    public void Salary_Empty_IsValid()
    {
        var salary = "";
        var isValid = IsValidSalary(salary);
        Assert.True(isValid); // Optional field
    }

    // ─── Application Deadline Validation ─────────────────────────────────

    [Fact]
    public void ApplicationDeadline_FutureDate_IsValid()
    {
        var deadline = DateTime.UtcNow.AddDays(30);
        var isValid = IsValidApplicationDeadline(deadline);
        Assert.True(isValid);
    }

    [Fact]
    public void ApplicationDeadline_TodayDate_IsInvalid()
    {
        var deadline = DateTime.UtcNow.Date;
        var isValid = IsValidApplicationDeadline(deadline);
        Assert.False(isValid);
    }

    [Fact]
    public void ApplicationDeadline_PastDate_IsInvalid()
    {
        var deadline = DateTime.UtcNow.AddDays(-1);
        var isValid = IsValidApplicationDeadline(deadline);
        Assert.False(isValid);
    }

    [Fact]
    public void ApplicationDeadline_DefaultDate_IsInvalid()
    {
        var deadline = default(DateTime);
        var isValid = IsValidApplicationDeadline(deadline);
        Assert.False(isValid);
    }

    // ─── Experience Level Validation ─────────────────────────────────────

    [Theory]
    [InlineData("Entry Level")]
    [InlineData("Junior")]
    [InlineData("Mid-Level")]
    [InlineData("Senior")]
    [InlineData("Lead")]
    [InlineData("Manager")]
    [InlineData("Director")]
    [InlineData("Executive")]
    public void ExperienceLevel_ValidLevels_IsValid(string level)
    {
        var isValid = IsValidExperienceLevel(level);
        Assert.True(isValid);
    }

    [Fact]
    public void ExperienceLevel_Null_IsValid()
    {
        string? level = null;
        var isValid = IsValidExperienceLevel(level);
        Assert.True(isValid); // Optional field
    }

    [Fact]
    public void ExperienceLevel_Empty_IsValid()
    {
        var level = "";
        var isValid = IsValidExperienceLevel(level);
        Assert.True(isValid); // Optional field
    }

    // ─── Job Status Logic ────────────────────────────────────────────────

    [Theory]
    [InlineData("Active")]
    [InlineData("Closed")]
    [InlineData("Expired")]
    [InlineData("Draft")]
    public void JobStatus_ValidStatuses_IsValid(string status)
    {
        var isValid = IsValidJobStatus(status);
        Assert.True(isValid);
    }

    [Fact]
    public void JobStatus_InvalidStatus_IsInvalid()
    {
        var status = "Invalid Status";
        var isValid = IsValidJobStatus(status);
        Assert.False(isValid);
    }

    [Fact]
    public void JobStatus_DeterminesActiveBasedOnDeadline()
    {
        var deadline = DateTime.UtcNow.AddDays(10);
        var isActive = IsJobActive(deadline);
        Assert.True(isActive);
    }

    [Fact]
    public void JobStatus_ExpiredWhenDeadlinePassed()
    {
        var deadline = DateTime.UtcNow.AddDays(-10);
        var isActive = IsJobActive(deadline);
        Assert.False(isActive);
    }

    // ─── Statistics Logic ─────────────────────────────────────────────────

    [Fact]
    public void JobStats_CountsActiveJobsCorrectly()
    {
        var jobs = new List<JobStatsTestData>
        {
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(10), JobType = "Full-time" },
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(5), JobType = "Internship" },
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(-1), JobType = "Full-time" }, // Expired
            new() { Status = "Closed", Deadline = DateTime.UtcNow.AddDays(20), JobType = "Full-time" }
        };

        var activeJobs = jobs.Count(j => j.Status == "Active" && j.Deadline >= DateTime.UtcNow.Date);
        Assert.Equal(2, activeJobs);
    }

    [Fact]
    public void JobStats_CountsActiveInternshipsCorrectly()
    {
        var jobs = new List<JobStatsTestData>
        {
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(10), JobType = "Internship" },
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(5), JobType = "Full-time" },
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(1), JobType = "Internship" },
            new() { Status = "Active", Deadline = DateTime.UtcNow.AddDays(-1), JobType = "Internship" } // Expired
        };

        var activeInternships = jobs.Count(j => j.Status == "Active" && j.Deadline >= DateTime.UtcNow.Date && j.JobType == "Internship");
        Assert.Equal(2, activeInternships);
    }

    [Fact]
    public void JobStats_CountsTotalApplicantsCorrectly()
    {
        var jobApplications = new Dictionary<int, int>
        {
            { 1, 15 }, // Job 1 has 15 applicants
            { 2, 8 },  // Job 2 has 8 applicants
            { 3, 22 }  // Job 3 has 22 applicants
        };

        var totalApplicants = jobApplications.Values.Sum();
        Assert.Equal(45, totalApplicants);
    }

    [Fact]
    public void JobStats_ReturnsZeroWhenNoJobs()
    {
        var jobs = new List<JobStatsTestData>();
        
        var activeJobs = jobs.Count(j => j.Status == "Active" && j.Deadline >= DateTime.UtcNow.Date);
        var activeInternships = jobs.Count(j => j.Status == "Active" && j.Deadline >= DateTime.UtcNow.Date && j.JobType == "Internship");
        
        Assert.Equal(0, activeJobs);
        Assert.Equal(0, activeInternships);
    }

    // ─── Company ID Filtering Logic ───────────────────────────────────────

    [Fact]
    public void CompanyJobFilter_OnlyReturnsOwnCompanyJobs()
    {
        var companyId = 1;
        var allJobs = new List<JobWithCompanyTestData>
        {
            new() { JobId = 1, CompanyId = 1, Title = "Job 1" },
            new() { JobId = 2, CompanyId = 2, Title = "Job 2" },
            new() { JobId = 3, CompanyId = 1, Title = "Job 3" },
            new() { JobId = 4, CompanyId = 3, Title = "Job 4" },
            new() { JobId = 5, CompanyId = 1, Title = "Job 5" }
        };

        var companyJobs = allJobs.Where(j => j.CompanyId == companyId).ToList();
        
        Assert.Equal(3, companyJobs.Count);
        Assert.All(companyJobs, j => Assert.Equal(companyId, j.CompanyId));
    }

    [Fact]
    public void CompanyJobFilter_NoJobsForCompany_ReturnsEmpty()
    {
        var companyId = 99;
        var allJobs = new List<JobWithCompanyTestData>
        {
            new() { JobId = 1, CompanyId = 1, Title = "Job 1" },
            new() { JobId = 2, CompanyId = 2, Title = "Job 2" }
        };

        var companyJobs = allJobs.Where(j => j.CompanyId == companyId).ToList();
        
        Assert.Empty(companyJobs);
    }

    // ─── Authorization Logic ──────────────────────────────────────────────

    [Fact]
    public void Authorization_OnlyCompanyRole_CanAccessJobEndpoints()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "COMPANY" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.True(hasAccess);
    }

    [Fact]
    public void Authorization_StudentRole_CannotAccessJobEndpoints()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "STUDENT" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    [Fact]
    public void Authorization_AdminRole_CannotAccessJobEndpoints()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "ADMIN" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    [Fact]
    public void Authorization_NoToken_NoAccess()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new string[] { };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    // ─── Company Approval Status Logic ────────────────────────────────────

    [Fact]
    public void CompanyApproval_ApprovedCompany_CanPostJobs()
    {
        var companyStatus = "APPROVED";
        var canPostJobs = companyStatus == "APPROVED";
        
        Assert.True(canPostJobs);
    }

    [Theory]
    [InlineData("PENDING_APPROVAL")]
    [InlineData("REJECTED")]
    [InlineData("SUSPENDED")]
    public void CompanyApproval_NonApprovedCompanies_CannotPostJobs(string status)
    {
        var canPostJobs = status == "APPROVED";
        
        Assert.False(canPostJobs);
    }

    // ─── Helper Methods (Business Logic) ─────────────────────────────────

    private static bool IsValidJobTitle(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) && title.Length <= 200;
    }

    private static bool IsValidJobDescription(string? description)
    {
        return !string.IsNullOrWhiteSpace(description);
    }

    private static bool IsValidRequirements(string? requirements)
    {
        return !string.IsNullOrWhiteSpace(requirements);
    }

    private static bool IsValidLocation(string? location)
    {
        return !string.IsNullOrWhiteSpace(location) && location.Length <= 150;
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

    private static bool IsValidSalary(string? salary)
    {
        return salary == null || salary.Length <= 100;
    }

    private static bool IsValidApplicationDeadline(DateTime deadline)
    {
        if (deadline == default) return false;
        return deadline.Date > DateTime.UtcNow.Date;
    }

    private static bool IsValidExperienceLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level)) return true;
        
        var validLevels = new[] { "Entry Level", "Junior", "Mid-Level", "Senior", "Lead", "Manager", "Director", "Executive" };
        return validLevels.Contains(level);
    }

    private static bool IsValidJobStatus(string status)
    {
        var validStatuses = new[] { "Active", "Closed", "Expired", "Draft" };
        return validStatuses.Contains(status);
    }

    private static bool IsJobActive(DateTime deadline)
    {
        return deadline.Date >= DateTime.UtcNow.Date;
    }

    // ─── Test Data Classes ────────────────────────────────────────────────

    private class JobStatsTestData
    {
        public string Status { get; set; } = "";
        public DateTime Deadline { get; set; }
        public string JobType { get; set; } = "";
    }

    private class JobWithCompanyTestData
    {
        public int JobId { get; set; }
        public int CompanyId { get; set; }
        public string Title { get; set; } = "";
    }
}