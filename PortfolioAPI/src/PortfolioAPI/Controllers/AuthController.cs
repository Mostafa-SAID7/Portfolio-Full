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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public IActionResult Login([FromBody] LoginDto? dto)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var configuredUser = _configuration["Auth:Username"];
            var configuredPassword = _configuration["Auth:Password"];

            if (string.IsNullOrWhiteSpace(configuredUser) || string.IsNullOrWhiteSpace(configuredPassword))
            {
                // Fail closed AND make the misconfiguration visible in the log
                // (review finding: auth outcomes were never logged at all).
                _logger.LogWarning(
                    "Login rejected ({RemoteIp}): Auth:Username/Auth:Password are not configured (503).", remoteIp);
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
                // Audit trail for brute-force forensics. Never log the submitted
                // password or the configured one — only the attempt and source.
                _logger.LogWarning(
                    "Login failed for username {AttemptedUsername} from {RemoteIp}.",
                    string.IsNullOrWhiteSpace(dto?.Username) ? "(empty)" : dto!.Username, remoteIp);
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var displayName = _configuration["Auth:DisplayName"];
            var email = _configuration["Auth:Email"];
            var (token, expiresAt) = _tokenService.IssueToken(
                string.IsNullOrWhiteSpace(displayName) ? configuredUser : displayName!,
                email ?? string.Empty);

            _logger.LogInformation(
                "Login succeeded for {Username} from {RemoteIp} (token expires {ExpiresAt:u}).",
                configuredUser, remoteIp, expiresAt);

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
