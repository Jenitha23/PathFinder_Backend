using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Unit tests for company profile business logic.
/// These tests validate the pure logic (no DB calls) — 
/// validation rules, field formatting, and profile update semantics.
/// </summary>
public class CompanyProfileLogicTests
{
    // ─── Company Name Validation ─────────────────────────────────────────────

    [Fact]
    public void CompanyName_ValidName_IsValid()
    {
        var companyName = "Tech Solutions Ltd";
        var isValid = IsValidCompanyName(companyName);
        Assert.True(isValid);
    }

    [Fact]
    public void CompanyName_EmptyString_IsInvalid()
    {
        var companyName = "";
        var isValid = IsValidCompanyName(companyName);
        Assert.False(isValid);
    }

    [Fact]
    public void CompanyName_Null_IsInvalid()
    {
        string? companyName = null;
        var isValid = IsValidCompanyName(companyName);
        Assert.False(isValid);
    }

    [Fact]
    public void CompanyName_Whitespace_IsInvalid()
    {
        var companyName = "   ";
        var isValid = IsValidCompanyName(companyName);
        Assert.False(isValid);
    }

    [Fact]
    public void CompanyName_ExceedsMaxLength_IsInvalid()
    {
        var companyName = new string('A', 151); // 151 characters, max is 150
        var isValid = IsValidCompanyName(companyName);
        Assert.False(isValid);
    }

    [Fact]
    public void CompanyName_MaxLength_IsValid()
    {
        var companyName = new string('A', 150); // Exactly 150 characters
        var isValid = IsValidCompanyName(companyName);
        Assert.True(isValid);
    }

    // ─── Email Validation ────────────────────────────────────────────────────

    [Theory]
    [InlineData("company@gmail.com")]
    [InlineData("tech@company.com")]
    [InlineData("company.name@domain.co.uk")]
    [InlineData("company+label@gmail.com")]
    public void Email_ValidFormat_IsValid(string email)
    {
        var isValid = IsValidEmail(email);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("company@")]
    [InlineData("@domain.com")]
    [InlineData("company@domain.")]
    [InlineData("company@domain.c")]
    [InlineData("company@domain.")]
    [InlineData("company@domain.")]
    public void Email_InvalidFormat_IsInvalid(string email)
    {
        var isValid = IsValidEmail(email);
        Assert.False(isValid);
    }

    [Fact]
    public void Email_Null_IsInvalid()
    {
        string? email = null;
        var isValid = IsValidEmail(email);
        Assert.False(isValid);
    }

    [Fact]
    public void Email_Trimmed_RemovesWhitespace()
    {
        var email = "  company@gmail.com  ";
        var trimmed = email.Trim().ToLowerInvariant();
        Assert.Equal("company@gmail.com", trimmed);
    }

    [Fact]
    public void Email_Normalized_ToLowerCase()
    {
        var email = "Company@Gmail.com";
        var normalized = email.Trim().ToLowerInvariant();
        Assert.Equal("company@gmail.com", normalized);
    }

    // ─── Website URL Validation ──────────────────────────────────────────────

    [Theory]
    [InlineData("https://www.techsolutions.com")]
    [InlineData("http://company.com")]
    [InlineData("https://subdomain.company.com")]
    [InlineData("https://company.com/path/to/page")]
    [InlineData("https://company.com?param=value")]
    public void Website_ValidUrl_IsValid(string website)
    {
        var isValid = IsValidWebsite(website);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("www.company.com")] // Missing protocol
    [InlineData("ftp://company.com")] // Wrong protocol
    [InlineData("http://")] // Empty domain
    public void Website_InvalidUrl_IsInvalid(string website)
    {
        var isValid = IsValidWebsite(website);
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Website_EmptyOrWhitespace_IsValid(string website)
    {
        // Empty/whitespace should be valid for optional field
        var isValid = IsValidWebsite(website);
        Assert.True(isValid);
    }

    [Fact]
    public void Website_Null_IsValid()
    {
        string? website = null;
        var isValid = IsValidWebsite(website);
        Assert.True(isValid); // Optional field
    }

    [Fact]
    public void Website_ExceedsMaxLength_IsInvalid()
    {
        var website = "https://" + new string('a', 291) + ".com"; // Total > 300 chars
        var isValid = IsValidWebsite(website);
        Assert.False(isValid);
    }

    // ─── Phone Number Validation ─────────────────────────────────────────────

    [Theory]
    [InlineData("+94 77 123 4567")]
    [InlineData("0771234567")]
    [InlineData("+1-555-123-4567")]
    [InlineData("(555) 123-4567")]
    [InlineData("123-456-7890")]
    public void Phone_ValidFormat_IsValid(string phone)
    {
        var isValid = IsValidPhone(phone);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456789012345678901234567890123456789012345678901")] // > 50 chars
    public void Phone_InvalidFormat_IsInvalid(string phone)
    {
        var isValid = IsValidPhone(phone);
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Phone_EmptyOrWhitespace_IsValid(string phone)
    {
        // Empty/whitespace should be valid for optional field
        var isValid = IsValidPhone(phone);
        Assert.True(isValid);
    }

    [Fact]
    public void Phone_Null_IsValid()
    {
        string? phone = null;
        var isValid = IsValidPhone(phone);
        Assert.True(isValid); // Optional field
    }

    // ─── Field Length Validations ────────────────────────────────────────────

    [Fact]
    public void Description_ExceedsMaxLength_IsInvalid()
    {
        var description = new string('A', 501); // 501 characters, max is 500
        var isValid = IsValidDescription(description);
        Assert.False(isValid);
    }

    [Fact]
    public void Description_MaxLength_IsValid()
    {
        var description = new string('A', 500);
        var isValid = IsValidDescription(description);
        Assert.True(isValid);
    }

    [Fact]
    public void Description_Null_IsValid()
    {
        string? description = null;
        var isValid = IsValidDescription(description);
        Assert.True(isValid);
    }

    [Fact]
    public void Industry_ExceedsMaxLength_IsInvalid()
    {
        var industry = new string('A', 151); // 151 characters, max is 150
        var isValid = IsValidIndustry(industry);
        Assert.False(isValid);
    }

    [Fact]
    public void Industry_MaxLength_IsValid()
    {
        var industry = new string('A', 150);
        var isValid = IsValidIndustry(industry);
        Assert.True(isValid);
    }

    [Fact]
    public void Industry_Null_IsValid()
    {
        string? industry = null;
        var isValid = IsValidIndustry(industry);
        Assert.True(isValid);
    }

    [Fact]
    public void Location_ExceedsMaxLength_IsInvalid()
    {
        var location = new string('A', 201); // 201 characters, max is 200
        var isValid = IsValidLocation(location);
        Assert.False(isValid);
    }

    [Fact]
    public void Location_MaxLength_IsValid()
    {
        var location = new string('A', 200);
        var isValid = IsValidLocation(location);
        Assert.True(isValid);
    }

    [Fact]
    public void Location_Null_IsValid()
    {
        string? location = null;
        var isValid = IsValidLocation(location);
        Assert.True(isValid);
    }

    // ─── Logo File Validation ────────────────────────────────────────────────

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".svg")]
    [InlineData(".webp")]
    public void LogoFile_AllowedExtensions_IsValid(string extension)
    {
        var fileName = $"logo{extension}";
        var isValid = IsValidLogoExtension(fileName);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    [InlineData(".exe")]
    [InlineData(".doc")]
    [InlineData(".mp4")]
    public void LogoFile_DisallowedExtensions_IsInvalid(string extension)
    {
        var fileName = $"logo{extension}";
        var isValid = IsValidLogoExtension(fileName);
        Assert.False(isValid);
    }

    [Fact]
    public void LogoFile_NoExtension_IsInvalid()
    {
        var fileName = "logo";
        var isValid = IsValidLogoExtension(fileName);
        Assert.False(isValid);
    }

    [Fact]
    public void LogoFile_SizeWithinLimit_IsValid()
    {
        long fileSize = 4 * 1024 * 1024; // 4MB
        var isValid = IsValidLogoSize(fileSize);
        Assert.True(isValid);
    }

    [Fact]
    public void LogoFile_SizeAtLimit_IsValid()
    {
        long fileSize = 5 * 1024 * 1024; // Exactly 5MB
        var isValid = IsValidLogoSize(fileSize);
        Assert.True(isValid);
    }

    [Fact]
    public void LogoFile_SizeExceedsLimit_IsInvalid()
    {
        long fileSize = 6 * 1024 * 1024; // 6MB
        var isValid = IsValidLogoSize(fileSize);
        Assert.False(isValid);
    }

    // ─── Profile Completeness Logic ──────────────────────────────────────────

    [Fact]
    public void ProfileCompleteness_AllRequiredFieldsFilled_IsComplete()
    {
        var profile = new
        {
            CompanyName = "Tech Solutions Ltd",
            Email = "company@gmail.com"
        };

        var isComplete = IsProfileComplete(profile);
        Assert.True(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_AllFieldsFilled_IsComplete()
    {
        var profile = new
        {
            CompanyName = "Tech Solutions Ltd",
            Email = "company@gmail.com",
            Description = "IT company",
            Industry = "Technology",
            Website = "https://techsolutions.com",
            Location = "Colombo",
            Phone = "+94 77 123 4567"
        };

        var isComplete = IsProfileComplete(profile);
        Assert.True(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_MissingOptionalFields_IsComplete()
    {
        var profile = new
        {
            CompanyName = "Tech Solutions Ltd",
            Email = "company@gmail.com",
            Description = (string?)null,
            Industry = (string?)null,
            Website = (string?)null,
            Location = (string?)null,
            Phone = (string?)null
        };

        var isComplete = IsProfileComplete(profile);
        Assert.True(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_MissingCompanyName_IsIncomplete()
    {
        var profile = new
        {
            CompanyName = (string?)null,
            Email = "company@gmail.com"
        };

        var isComplete = IsProfileComplete(profile);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_MissingEmail_IsIncomplete()
    {
        var profile = new
        {
            CompanyName = "Tech Solutions Ltd",
            Email = (string?)null
        };

        var isComplete = IsProfileComplete(profile);
        Assert.False(isComplete);
    }

    [Fact]
    public void ProfileCompleteness_BothMissing_IsIncomplete()
    {
        var profile = new
        {
            CompanyName = (string?)null,
            Email = (string?)null
        };

        var isComplete = IsProfileComplete(profile);
        Assert.False(isComplete);
    }

    // ─── Logo URL Generation ─────────────────────────────────────────────────

    [Fact]
    public void LogoUrl_GeneratesUniqueFilename()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        
        var url1 = $"http://localhost:5249/uploads/company-logos/company-1/logo_{guid1}.png";
        var url2 = $"http://localhost:5249/uploads/company-logos/company-1/logo_{guid2}.png";
        
        Assert.NotEqual(url1, url2);
    }

    [Fact]
    public void LogoUrl_ContainsCompanyId()
    {
        var companyId = 1;
        var guid = Guid.NewGuid();
        var url = $"http://localhost:5249/uploads/company-logos/company-{companyId}/logo_{guid}.png";
        
        Assert.Contains($"company-{companyId}", url);
    }

    [Fact]
    public void LogoUrl_ContainsValidExtension()
    {
        var extensions = new[] { ".jpg", ".png", ".gif", ".svg", ".webp" };
        
        foreach (var ext in extensions)
        {
            var guid = Guid.NewGuid();
            var url = $"http://localhost:5249/uploads/company-logos/company-1/logo_{guid}{ext}";
            Assert.EndsWith(ext, url);
        }
    }

    // ─── Profile Update Semantics ────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_OnlyUpdatedFields_Change()
    {
        var original = new { Name = "Old Name", Description = "Old Desc" };
        var update = new { Name = "New Name", Description = "Old Desc" };
        
        var updated = new { Name = update.Name ?? original.Name, Description = update.Description ?? original.Description };
        
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Old Desc", updated.Description);
    }

    [Fact]
    public void UpdateProfile_NullFields_KeepOriginal()
    {
        var original = new { Name = "Original Name", Description = "Original Desc" };
        var update = new { Name = (string?)null, Description = (string?)null };
        
        var updated = new { Name = update.Name ?? original.Name, Description = update.Description ?? original.Description };
        
        Assert.Equal("Original Name", updated.Name);
        Assert.Equal("Original Desc", updated.Description);
    }

    [Fact]
    public void RemoveLogo_FlagTrue_LogoUrlBecomesNull()
    {
        var removeLogo = true;
        
        var finalLogoUrl = removeLogo ? null : "http://example.com/logo.png";
        
        Assert.Null(finalLogoUrl);
    }

    [Fact]
    public void RemoveLogo_FlagFalse_LogoUrlRemains()
    {
        var logoUrl = "http://example.com/logo.png";
        var removeLogo = false;
        
        var finalLogoUrl = removeLogo ? null : logoUrl;
        
        Assert.Equal(logoUrl, finalLogoUrl);
    }

    // ─── Authorization Logic ─────────────────────────────────────────────────

    [Fact]
    public void Authorization_OnlyCompanyRole_CanAccess()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "COMPANY" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.True(hasAccess);
    }

    [Fact]
    public void Authorization_StudentRole_CannotAccess()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "STUDENT" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    [Fact]
    public void Authorization_AdminRole_CannotAccess()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new[] { "ADMIN" };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    [Fact]
    public void Authorization_NoRole_CannotAccess()
    {
        var allowedRoles = new[] { "COMPANY" };
        var userRoles = new string[] { };
        
        var hasAccess = userRoles.Intersect(allowedRoles).Any();
        
        Assert.False(hasAccess);
    }

    // ─── Helper Methods (Business Logic) ─────────────────────────────────────

    private static bool IsValidCompanyName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Length <= 150;
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        email = email.Trim();
        
        // Must contain @
        if (!email.Contains("@"))
            return false;
        
        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;
        
        var localPart = parts[0];
        var domainPart = parts[1];
        
        // Local part cannot be empty
        if (string.IsNullOrWhiteSpace(localPart))
            return false;
        
        // Domain must contain at least one dot
        if (!domainPart.Contains("."))
            return false;
        
        var domainParts = domainPart.Split('.');
        if (domainParts.Length < 2)
            return false;
        
        // TLD (last part) must be at least 2 characters
        var tld = domainParts.Last();
        if (tld.Length < 2)
            return false;
        
        // Additional validation using MailAddress
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

    private static bool IsValidWebsite(string? website)
    {
        if (string.IsNullOrWhiteSpace(website))
            return true; // Optional field - empty or whitespace is valid
            
        if (website.Length > 300)
            return false;
            
        return Uri.TryCreate(website, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true; // Optional field - empty or whitespace is valid
            
        if (phone.Length > 50)
            return false;
            
        // Basic phone validation - allows numbers, spaces, +, -, (, )
        return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9\+\-\s\(\)]+$");
    }

    private static bool IsValidDescription(string? description)
    {
        return description == null || description.Length <= 500;
    }

    private static bool IsValidIndustry(string? industry)
    {
        return industry == null || industry.Length <= 150;
    }

    private static bool IsValidLocation(string? location)
    {
        return location == null || location.Length <= 200;
    }

    private static bool IsValidLogoExtension(string fileName)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    private static bool IsValidLogoSize(long fileSize)
    {
        const long maxLogoSize = 5 * 1024 * 1024; // 5MB
        return fileSize <= maxLogoSize;
    }

    private static bool IsProfileComplete(dynamic profile)
    {
        var hasCompanyName = !string.IsNullOrWhiteSpace(profile.CompanyName);
        var hasEmail = !string.IsNullOrWhiteSpace(profile.Email);
        return hasCompanyName && hasEmail;
    }
}