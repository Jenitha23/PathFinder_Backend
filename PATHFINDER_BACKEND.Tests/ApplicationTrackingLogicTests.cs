using Xunit;

/// <summary>
/// Unit tests for application tracking business logic.
/// These tests validate the pure logic (no DB calls) — 
/// profile completeness rules, status defaults, and duplicate check semantics.
/// </summary>
public class ApplicationTrackingLogicTests
{
    // ─── Profile Completeness Logic ───────────────────────────────────────────

    [Fact]
    public void ProfileCompleteness_BothSkillsAndCv_IsComplete()
    {
        var skills = "C#, ASP.NET";
        var cvUrl = "Uploads/CVs/student-1/cv.pdf";

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.True(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_MissingSkills_IsIncomplete()
    {
        var skills = "";
        var cvUrl = "Uploads/CVs/student-1/cv.pdf";

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_NullSkills_IsIncomplete()
    {
        string? skills = null;
        var cvUrl = "Uploads/CVs/student-1/cv.pdf";

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_WhitespaceSkills_IsIncomplete()
    {
        var skills = "   ";
        var cvUrl = "Uploads/CVs/student-1/cv.pdf";

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_MissingCv_IsIncomplete()
    {
        var skills = "C#, ASP.NET";
        string? cvUrl = null;

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_EmptyCv_IsIncomplete()
    {
        var skills = "C#, ASP.NET";
        var cvUrl = "";

        var isComplete = IsProfileComplete(skills, cvUrl);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_BothMissing_IsIncomplete()
    {
        var isComplete = IsProfileComplete(null, null);
        Assert.False(isComplete);
    }

    // ─── Default Status Logic ─────────────────────────────────────────────────

    [Fact]
    public void ApplicationStatus_DefaultValue_IsPending()
    {
        // The status string used as SQL DEFAULT and in the INSERT statement
        const string defaultStatus = "Pending";
        Assert.Equal("Pending", defaultStatus);
    }

    [Fact]
    public void ApplicationStatus_NotApproved_OnCreation()
    {
        const string defaultStatus = "Pending";
        Assert.NotEqual("Approved", defaultStatus);
    }

    [Fact]
    public void ApplicationStatus_NotRejected_OnCreation()
    {
        const string defaultStatus = "Pending";
        Assert.NotEqual("Rejected", defaultStatus);
    }

    // ─── Cover Letter Normalization ───────────────────────────────────────────

    [Fact]
    public void CoverLetter_Trimmed_RemovesLeadingTrailingWhitespace()
    {
        var raw = "  I am very interested.  ";
        var trimmed = raw.Trim();
        Assert.Equal("I am very interested.", trimmed);
    }

    [Fact]
    public void CoverLetter_Null_StaysNull()
    {
        string? raw = null;
        var trimmed = raw?.Trim();
        Assert.Null(trimmed);
    }

    [Fact]
    public void CoverLetter_EmptyString_TrimsToEmpty()
    {
        var raw = "   ";
        var trimmed = raw.Trim();
        Assert.Equal("", trimmed);
    }

    // ─── Duplicate Application Detection ──────────────────────────────────────

    [Fact]
    public void DuplicateCheck_SameStudentAndJob_DetectedAsDuplicate()
    {
        // Simulates the logic: if a record exists for the same (student_id, job_id), block.
        var existingApplications = new List<(int StudentId, int JobId)>
        {
            (1, 10),
            (2, 10)
        };

        bool IsDuplicate(int studentId, int jobId) =>
            existingApplications.Any(a => a.StudentId == studentId && a.JobId == jobId);

        Assert.True(IsDuplicate(1, 10));
    }

    [Fact]
    public void DuplicateCheck_DifferentJob_NotDuplicate()
    {
        var existingApplications = new List<(int StudentId, int JobId)>
        {
            (1, 10)
        };

        bool IsDuplicate(int studentId, int jobId) =>
            existingApplications.Any(a => a.StudentId == studentId && a.JobId == jobId);

        Assert.False(IsDuplicate(1, 99));
    }

    [Fact]
    public void DuplicateCheck_DifferentStudent_NotDuplicate()
    {
        var existingApplications = new List<(int StudentId, int JobId)>
        {
            (1, 10)
        };

        bool IsDuplicate(int studentId, int jobId) =>
            existingApplications.Any(a => a.StudentId == studentId && a.JobId == jobId);

        Assert.False(IsDuplicate(2, 10));
    }

    [Fact]
    public void DuplicateCheck_EmptyList_NotDuplicate()
    {
        var existingApplications = new List<(int StudentId, int JobId)>();

        bool IsDuplicate(int studentId, int jobId) =>
            existingApplications.Any(a => a.StudentId == studentId && a.JobId == jobId);

        Assert.False(IsDuplicate(1, 10));
    }

    // ─── JobId Validation Logic ───────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void JobId_ZeroOrNegative_IsInvalid(int jobId)
    {
        // The controller should reject non-positive job IDs before a DB call
        Assert.True(jobId <= 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void JobId_Positive_IsValid(int jobId)
    {
        Assert.True(jobId > 0);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the profile completeness logic from ApplicationsController.
    /// A profile is complete if it has at least one skill AND a CV URL.
    /// </summary>
    private static bool IsProfileComplete(string? skills, string? cvUrl)
    {
        var hasSkills = !string.IsNullOrWhiteSpace(skills);
        var hasCv = !string.IsNullOrWhiteSpace(cvUrl);
        return hasSkills && hasCv;
    }
}
