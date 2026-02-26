using System.ComponentModel.DataAnnotations;
using PATHFINDER_BACKEND.DTOs;
using Xunit;

public class AdminDtoValidationTests
{
    [Fact]
    public void AdminLoginRequest_EmptyFields_IsInvalid()
    {
        var model = new AdminLoginRequest
        {
            Email = "",
            Password = ""
        };

        var results = Validate(model);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AdminLoginRequest.Email)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AdminLoginRequest.Password)));
    }

    [Fact]
    public void AdminLoginRequest_InvalidEmail_IsInvalid()
    {
        var model = new AdminLoginRequest
        {
            Email = "not-an-email",
            Password = "Admin@123"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AdminLoginRequest.Email)));
    }

    [Fact]
    public void AdminLoginRequest_ValidData_IsValid()
    {
        var model = new AdminLoginRequest
        {
            Email = "admin@pathfinder.com",
            Password = "Admin@123"
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void AdminUpdateProfileRequest_InvalidEmail_IsInvalid()
    {
        var model = new AdminUpdateProfileRequest
        {
            FullName = "System Admin",
            Email = "bad-email"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AdminUpdateProfileRequest.Email)));
    }

    [Fact]
    public void AdminChangePasswordRequest_ShortNewPassword_IsInvalid()
    {
        var model = new AdminChangePasswordRequest
        {
            CurrentPassword = "Current@123",
            NewPassword = "short"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AdminChangePasswordRequest.NewPassword)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
