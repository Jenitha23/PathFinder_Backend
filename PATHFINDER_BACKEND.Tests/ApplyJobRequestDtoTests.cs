using System.ComponentModel.DataAnnotations;
using PATHFINDER_BACKEND.DTOs;
using Xunit;

/// <summary>
/// Unit tests for ApplyJobRequest DTO validation.
/// Tests ensure the request model is correctly structured.
/// </summary>
public class ApplyJobRequestDtoTests
{
    // ─── JobId Validation ────────────────────────────────────────────────────

    [Fact]
    public void ApplyJobRequest_DefaultJobId_IsZero()
    {
        var req = new ApplyJobRequest();
        Assert.Equal(0, req.JobId);
    }

    [Fact]
    public void ApplyJobRequest_ValidJobId_Assigned()
    {
        var req = new ApplyJobRequest { JobId = 5 };
        Assert.Equal(5, req.JobId);
    }

    [Fact]
    public void ApplyJobRequest_NegativeJobId_IsStored()
    {
        // DTO has no data annotations; controller validates business rules.
        // Here we verify the DTO stores whatever int is given.
        var req = new ApplyJobRequest { JobId = -1 };
        Assert.Equal(-1, req.JobId);
    }

    // ─── CoverLetter Validation ───────────────────────────────────────────────

    [Fact]
    public void ApplyJobRequest_DefaultCoverLetter_IsNull()
    {
        var req = new ApplyJobRequest { JobId = 1 };
        Assert.Null(req.CoverLetter);
    }

    [Fact]
    public void ApplyJobRequest_WithCoverLetter_Assigned()
    {
        var req = new ApplyJobRequest
        {
            JobId = 1,
            CoverLetter = "I am very interested in this role."
        };
        Assert.Equal("I am very interested in this role.", req.CoverLetter);
    }

    [Fact]
    public void ApplyJobRequest_EmptyCoverLetter_IsAllowed()
    {
        // CoverLetter is optional — empty string should be accepted at DTO level.
        var req = new ApplyJobRequest { JobId = 1, CoverLetter = "" };
        Assert.Equal("", req.CoverLetter);
    }

    [Fact]
    public void ApplyJobRequest_LongCoverLetter_IsStoredFully()
    {
        var longText = new string('A', 5000);
        var req = new ApplyJobRequest { JobId = 1, CoverLetter = longText };
        Assert.Equal(5000, req.CoverLetter!.Length);
    }

    // ─── Full Object Validation ───────────────────────────────────────────────

    [Fact]
    public void ApplyJobRequest_ValidObject_PassesAnnotationValidation()
    {
        var req = new ApplyJobRequest
        {
            JobId = 1,
            CoverLetter = "I am interested."
        };

        var results = ValidateModel(req);
        Assert.Empty(results); // No data annotations, so should pass
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
