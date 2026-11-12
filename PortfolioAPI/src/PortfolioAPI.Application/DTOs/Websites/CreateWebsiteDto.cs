namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted when creating a website (POST api/Websites).</summary>
    public class CreateWebsiteDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? BaseUrl { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
