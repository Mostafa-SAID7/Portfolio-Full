using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PortfolioAPI.Auth
{
    public interface ITokenService
    {
        /// <summary>Security key used to sign AND validate tokens (shared with the JWT bearer middleware).</summary>
        SecurityKey SigningKey { get; }

        (string Token, DateTime ExpiresAt) IssueToken(string username, string email);
    }

    /// <summary>
    /// Issues short-lived HMAC-SHA256 JWTs for the admin dashboard.
    /// Signing key resolution (issues #1/#6):
    ///   - Auth:SigningKey configured → used (must be ≥ 32 bytes).
    ///   - missing → a random per-process key (tokens die on restart;
    ///     nothing secret ships in any config).
    /// Credentials (Auth:Username/Auth:Password) are validated in AuthController —
    /// empty values make login fail closed with 503 until configured.
    /// </summary>
    public sealed class TokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _tokenHours;

        public SecurityKey SigningKey { get; }

        public TokenService(IConfiguration configuration)
        {
            _issuer = configuration["Auth:Issuer"] ?? "PortfolioAPI";
            _audience = configuration["Auth:Audience"] ?? "PortfolioDashboard";
            // Default token lifetime tightened 12 h → 2 h (short-lived JWT; override
        // with Auth:TokenHours). The dashboard recovers from expiry cleanly —
        // the interceptor clears the session and prompts a re-login.
        _tokenHours = configuration.GetValue("Auth:TokenHours", 2);

            var configuredKey = configuration["Auth:SigningKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                // Ephemeral fallback: random per process, never logged, never committed.
                configuredKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            }
            else if (Encoding.UTF8.GetByteCount(configuredKey) < 32)
            {
                throw new InvalidOperationException(
                    "Auth:SigningKey must be at least 32 bytes. Generate one, e.g.: " +
                    "dotnet dev-certs (or) Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).");
            }

            SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredKey));
        }

        public (string Token, DateTime ExpiresAt) IssueToken(string username, string email)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddHours(_tokenHours);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, username),
                new("name", username),
                new("email", email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
