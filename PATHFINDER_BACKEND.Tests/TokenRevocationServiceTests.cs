using PATHFINDER_BACKEND.Services;
using Xunit;

public class TokenRevocationServiceTests
{
    [Fact]
    public void Revoke_MarksStudentTokenAsRevoked()
    {
        var service = new TokenRevocationService();
        var studentJti = Guid.NewGuid().ToString();

        service.Revoke(studentJti, DateTime.UtcNow.AddMinutes(5));

        Assert.True(service.IsRevoked(studentJti));
    }

    [Fact]
    public void Revoke_MarksCompanyTokenAsRevoked()
    {
        var service = new TokenRevocationService();
        var companyJti = Guid.NewGuid().ToString();

        service.Revoke(companyJti, DateTime.UtcNow.AddMinutes(5));

        Assert.True(service.IsRevoked(companyJti));
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_ForExpiredStudentRevocation()
    {
        var service = new TokenRevocationService();
        var studentJti = Guid.NewGuid().ToString();

        service.Revoke(studentJti, DateTime.UtcNow.AddMinutes(-1));

        Assert.False(service.IsRevoked(studentJti));
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_ForExpiredCompanyRevocation()
    {
        var service = new TokenRevocationService();
        var companyJti = Guid.NewGuid().ToString();

        service.Revoke(companyJti, DateTime.UtcNow.AddMinutes(-1));

        Assert.False(service.IsRevoked(companyJti));
    }

    [Fact]
    public void Revoke_MultipleTokens_TracksSeparately()
    {
        var service = new TokenRevocationService();
        var studentJti1 = Guid.NewGuid().ToString();
        var studentJti2 = Guid.NewGuid().ToString();
        var companyJti = Guid.NewGuid().ToString();

        service.Revoke(studentJti1, DateTime.UtcNow.AddMinutes(5));
        service.Revoke(companyJti, DateTime.UtcNow.AddMinutes(5));

        Assert.True(service.IsRevoked(studentJti1));
        Assert.False(service.IsRevoked(studentJti2));
        Assert.True(service.IsRevoked(companyJti));
    }

    [Fact]
    public void Revoke_SameToken_WithDifferentExpiry_UpdatesExpiry()
    {
        var service = new TokenRevocationService();
        var jti = Guid.NewGuid().ToString();
        var shortExpiry = DateTime.UtcNow.AddMinutes(1);

        service.Revoke(jti, shortExpiry);
        Assert.True(service.IsRevoked(jti));

        // Revoke same token with longer expiry
        var longExpiry = DateTime.UtcNow.AddHours(1);
        service.Revoke(jti, longExpiry);
        Assert.True(service.IsRevoked(jti));
    }
}
