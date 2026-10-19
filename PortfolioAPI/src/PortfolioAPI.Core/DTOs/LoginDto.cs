namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Credentials for POST /api/auth/login (admin dashboard sign-in).</summary>
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
