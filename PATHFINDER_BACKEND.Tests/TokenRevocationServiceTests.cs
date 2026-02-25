using PATHFINDER_BACKEND.Services;
using Xunit;

public class TokenRevocationServiceTests
{
    [Fact]
    public void Revoke_MarksTokenAsRevoked()
    {
        var service = new TokenRevocationService();
        var jti = Guid.NewGuid().ToString();

        service.Revoke(jti, DateTime.UtcNow.AddMinutes(5));

        Assert.True(service.IsRevoked(jti));
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_ForExpiredRevocation()
    {
        var service = new TokenRevocationService();
        var jti = Guid.NewGuid().ToString();

        service.Revoke(jti, DateTime.UtcNow.AddMinutes(-1));

        Assert.False(service.IsRevoked(jti));
    }
}
