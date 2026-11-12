namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Payload accepted by POST api/Contact/{id}/reply.</summary>
    public class ReplyToContactDto
    {
        public string Body { get; set; } = string.Empty;
    }
}
