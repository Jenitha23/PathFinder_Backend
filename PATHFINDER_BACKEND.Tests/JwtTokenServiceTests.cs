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
    public void CreateToken_StudentRole_ReturnsToken_WithExpectedClaims()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 10, email: "test@student.com", role: "STUDENT", fullName: "John Doe");

        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type.EndsWith("/role") && c.Value == "STUDENT");
        Assert.Contains(jwtToken.Claims, c => c.Type == "userId" && c.Value == "10");
        Assert.Contains(jwtToken.Claims, c => c.Type == "fullName" && c.Value == "John Doe");
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "test@student.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(c.Value));
    }

    [Fact]
    public void CreateToken_CompanyRole_ReturnsToken_WithExpectedClaims()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 5, email: "hr@company.com", role: "COMPANY", fullName: "Tech Corp");

        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type.EndsWith("/role") && c.Value == "COMPANY");
        Assert.Contains(jwtToken.Claims, c => c.Type == "userId" && c.Value == "5");
        Assert.Contains(jwtToken.Claims, c => c.Type == "fullName" && c.Value == "Tech Corp");
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "hr@company.com");
    }

    [Fact]
    public void CreateToken_ValidatesSuccessfully_WithIssuerAudienceKey_Student()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 5, email: "student@example.com", role: "STUDENT", fullName: "Alice Smith");

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
    public void CreateToken_ValidatesSuccessfully_WithIssuerAudienceKey_Company()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 20, email: "recruiter@techcorp.com", role: "COMPANY", fullName: "TechCorp Ltd");

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
    public void ReadJtiAndExpiry_ReturnsJtiAndExpiry_ForStudent()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 15, email: "jti@student.com", role: "STUDENT", fullName: "Student User");
        var result = jwt.ReadJtiAndExpiry(token);

        Assert.False(string.IsNullOrWhiteSpace(result.jti));
        Assert.NotNull(result.expiresUtc);
        Assert.True(result.expiresUtc > DateTime.UtcNow);
    }

    [Fact]
    public void ReadJtiAndExpiry_ReturnsJtiAndExpiry_ForCompany()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token = jwt.CreateToken(userId: 25, email: "jti@company.com", role: "COMPANY", fullName: "Company Name");
        var result = jwt.ReadJtiAndExpiry(token);

        Assert.False(string.IsNullOrWhiteSpace(result.jti));
        Assert.NotNull(result.expiresUtc);
        Assert.True(result.expiresUtc > DateTime.UtcNow);
    }

    [Fact]
    public void CreateToken_DifferentUsersHaveDifferentJti()
    {
        var config = BuildConfig();
        var jwt = new JwtTokenService(config);

        var token1 = jwt.CreateToken(userId: 1, email: "user1@student.com", role: "STUDENT", fullName: "User One");
        var token2 = jwt.CreateToken(userId: 2, email: "user2@student.com", role: "STUDENT", fullName: "User Two");

        var jti1 = jwt.ReadJtiAndExpiry(token1).jti;
        var jti2 = jwt.ReadJtiAndExpiry(token2).jti;

        Assert.NotEqual(jti1, jti2);
    }
}
