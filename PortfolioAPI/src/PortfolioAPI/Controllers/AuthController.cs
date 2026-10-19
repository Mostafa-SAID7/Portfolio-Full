using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioAPI.Auth;
using PortfolioAPI.Core.DTOs;

namespace PortfolioAPI.Controllers
{
    /// <summary>
    /// Admin sign-in (issues #1/#6/#12): validates the configured
    /// Auth:Username / Auth:Password and returns a JWT the dashboard sends as
    /// an Authorization header on every admin request.
    ///   - Unconfigured server (empty credentials) → 503 with instructions
    ///     (fail closed; no default password ever ships).
    ///   - Wrong credentials → 401 { message } (constant-time comparison).
    ///   - Login attempts are rate limited per IP (issue #10).
    /// The endpoint itself is anonymous — everything else admin is [Authorize].
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public AuthController(IConfiguration configuration, ITokenService tokenService)
        {
            _configuration = configuration;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public IActionResult Login([FromBody] LoginDto? dto)
        {
            var configuredUser = _configuration["Auth:Username"];
            var configuredPassword = _configuration["Auth:Password"];

            if (string.IsNullOrWhiteSpace(configuredUser) || string.IsNullOrWhiteSpace(configuredPassword))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Authentication is not configured on this server. Set Auth:Username and Auth:Password "
                            + "in appsettings.Production.json (or via Auth__Username / Auth__Password environment variables)."
                });
            }

            var userOk = FixedTimeEquals(dto?.Username, configuredUser);
            var passwordOk = FixedTimeEquals(dto?.Password, configuredPassword);

            if (!userOk || !passwordOk)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var displayName = _configuration["Auth:DisplayName"];
            var email = _configuration["Auth:Email"];
            var (token, expiresAt) = _tokenService.IssueToken(
                string.IsNullOrWhiteSpace(displayName) ? configuredUser : displayName!,
                email ?? string.Empty);

            return Ok(new
            {
                token,
                expiresAt,
                user = new
                {
                    id = 1,
                    name = string.IsNullOrWhiteSpace(displayName) ? configuredUser : displayName,
                    email = email ?? string.Empty
                }
            });
        }

        /// <summary>
        /// Length-safe constant-time comparison (both sides hashed to a fixed
        /// size first) so response timing never reveals which part was wrong.
        /// </summary>
        private static bool FixedTimeEquals(string? provided, string? configured)
        {
            var left = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? string.Empty));
            var right = SHA256.HashData(Encoding.UTF8.GetBytes(configured ?? string.Empty));
            return CryptographicOperations.FixedTimeEquals(left, right);
        }
    }
}
