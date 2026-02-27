using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using PATHFINDER_BACKEND.Services;
using Xunit;

public class AdminSecurityServiceTests
{
    private static IConfiguration BuildConfig()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    [Fact]
    public void CreateToken_AdminRole_HasAdminRoleClaim()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 1, email: "admin@pathfinder.com", role: "ADMIN", fullName: "System Admin");
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type.EndsWith("/role") && c.Value == "ADMIN");
        Assert.Contains(jwtToken.Claims, c => c.Type == "userId" && c.Value == "1");
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "admin@pathfinder.com");
    }

    [Fact]
    public void PasswordService_AdminPassword_HashAndVerify_Works()
    {
        var service = new PasswordService();
        var password = "Admin@123";

        var hash = service.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.True(service.Verify(password, hash));
        Assert.False(service.Verify("WrongAdmin@123", hash));
    }
}
