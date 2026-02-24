using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PATHFINDER_BACKEND.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(int userId, string email, string role, string fullName)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");
            var issuer = _config["Jwt:Issuer"] ?? "PathFinder";
            var audience = _config["Jwt:Audience"] ?? "PathFinderUsers";
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "120");
            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(ClaimTypes.Role, role),
                new Claim("fullName", fullName),
                new Claim("userId", userId.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string? jti, DateTime? expiresUtc) ReadJtiAndExpiry(string jwtToken)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                return (null, null);
            }

            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);
            var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            return (jti, token.ValidTo);
        }
    }
}
