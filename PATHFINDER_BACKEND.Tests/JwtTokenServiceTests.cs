using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using PATHFINDER_BACKEND.Services;


public class JwtTokenServiceTests
{
    private static IConfiguration BuildConfig()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
    }

    [Fact]
    public void CreateToken_ReturnsToken_WithExpectedClaims()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 10, email: "test@student.com", role: "STUDENT", fullName: "Test Student");

        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // check role claim
        Assert.Contains(jwtToken.Claims, c => c.Type.EndsWith("/role") && c.Value == "STUDENT");

        // check custom userId and fullName
        Assert.Contains(jwtToken.Claims, c => c.Type == "userId" && c.Value == "10");
        Assert.Contains(jwtToken.Claims, c => c.Type == "fullName" && c.Value == "Test Student");
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(c.Value));
    }

    [Fact]
    public void CreateToken_ValidatesSuccessfully_WithIssuerAudienceKey()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 5, email: "a@b.com", role: "STUDENT", fullName: "A B");

        var key = config["Jwt:Key"]!;
        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = System.TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, validationParams, out var validatedToken);

        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void ReadJtiAndExpiry_ReturnsJtiAndExpiry()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 15, email: "jti@student.com", role: "STUDENT", fullName: "Jti Student");
        var result = jwt.ReadJtiAndExpiry(token);

        Assert.False(string.IsNullOrWhiteSpace(result.jti));
        Assert.NotNull(result.expiresUtc);
    }
}
