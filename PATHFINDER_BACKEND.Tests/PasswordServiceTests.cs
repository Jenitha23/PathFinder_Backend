using PATHFINDER_BACKEND.Services;
using Xunit;

public class PasswordServiceTests
{
    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var svc = new PasswordService();

        var password = "Pass@1234";
        var hash = svc.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.True(svc.Verify(password, hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var svc = new PasswordService();

        var hash = svc.Hash("CorrectPass@1234");

        Assert.False(svc.Verify("WrongPass@1234", hash));
    }
}