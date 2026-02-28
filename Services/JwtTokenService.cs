using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PATHFINDER_BACKEND.Services
{
    /// <summary>
    /// Responsible for creating and reading JWT tokens.
    /// Keeps token logic centralized and consistent across roles (Student/Company/Admin).
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Creates a signed JWT with role and identity claims.
        /// Why we add Role claim:
        /// - ASP.NET Core [Authorize(Roles="...")] relies on a role claim.
        /// Why we add JTI:
        /// - Used for token revocation (logout) before expiry.
        /// </summary>
        public string CreateToken(int userId, string email, string role, string fullName)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");
            var issuer = _config["Jwt:Issuer"] ?? "PathFinder";
            var audience = _config["Jwt:Audience"] ?? "PathFinderUsers";
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "120");

            // JTI uniquely identifies this token instance (useful for revocation).
            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                // Standard subject claim (recommended JWT practice).
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),

                // Standard email claim (JWT registered claim name).
                new Claim(JwtRegisteredClaimNames.Email, email),

                // Unique token id for logout/revocation.
                new Claim(JwtRegisteredClaimNames.Jti, jti),

                // Role claim enables [Authorize(Roles="STUDENT")] etc.
                new Claim(ClaimTypes.Role, role),

                // Helpful app-specific claims
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

        /// <summary>
        /// Reads jti and expiry from a token string without validating it.
        /// Used by logout flow to revoke the current token.
        /// NOTE: Token validation happens in JWT middleware during normal requests.
        /// </summary>
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