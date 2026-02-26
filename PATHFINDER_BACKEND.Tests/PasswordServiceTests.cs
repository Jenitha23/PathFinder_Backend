using PATHFINDER_BACKEND.Services;
using Xunit;

public class PasswordServiceTests
{
    [Fact]
    public void Hash_ThenVerify_StudentPassword_WithCorrectPassword_ReturnsTrue()
    {
        var svc = new PasswordService();

        var studentPassword = "Student@Pass123";
        var hash = svc.Hash(studentPassword);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.True(svc.Verify(studentPassword, hash));
    }

    [Fact]
    public void Hash_ThenVerify_CompanyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var svc = new PasswordService();

        var companyPassword = "Company@Secure456";
        var hash = svc.Hash(companyPassword);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.True(svc.Verify(companyPassword, hash));
    }

    [Fact]
    public void Verify_StudentPassword_WithWrongPassword_ReturnsFalse()
    {
        var svc = new PasswordService();

        var hash = svc.Hash("CorrectStudentPass@1234");

        Assert.False(svc.Verify("WrongStudentPass@1234", hash));
    }

    [Fact]
    public void Verify_CompanyPassword_WithWrongPassword_ReturnsFalse()
    {
        var svc = new PasswordService();

        var hash = svc.Hash("CorrectCompanyPass@5678");

        Assert.False(svc.Verify("WrongCompanyPass@5678", hash));
    }

    [Fact]
    public void Hash_GeneratesDifferentHashes_ForSamePassword()
    {
        var svc = new PasswordService();
        var password = "SamePassword@123";

        var hash1 = svc.Hash(password);
        var hash2 = svc.Hash(password);

        // Same password should produce different hashes (due to salt)
        Assert.NotEqual(hash1, hash2);
        // But both should verify correctly
        Assert.True(svc.Verify(password, hash1));
        Assert.True(svc.Verify(password, hash2));
    }

    [Fact]
    public void Hash_WithSpecialCharacters_VerifiesCorrectly()
    {
        var svc = new PasswordService();
        var complexPassword = "P@$$w0rd!#%&*~";

        var hash = svc.Hash(complexPassword);

        Assert.True(svc.Verify(complexPassword, hash));
        Assert.False(svc.Verify("P@$$w0rd!#%&*", hash)); // Missing ~
    }

    [Fact]
    public void Hash_WithLongPassword_VerifiesCorrectly()
    {
        var svc = new PasswordService();
        var longPassword = "ThisIsAVeryLongPasswordWithManyCharactersThatShouldStillWorkCorrectly@123!";

        var hash = svc.Hash(longPassword);

        Assert.True(svc.Verify(longPassword, hash));
    }
}