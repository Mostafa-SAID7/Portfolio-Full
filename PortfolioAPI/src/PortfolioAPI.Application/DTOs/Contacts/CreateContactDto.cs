namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted when submitting the public contact form.</summary>
    public class CreateContactDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
