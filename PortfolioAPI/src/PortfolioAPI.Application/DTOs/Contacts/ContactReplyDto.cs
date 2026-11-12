using System;

namespace PortfolioAPI.Application.DTOs
{
    /// <summary>Read model for a reply the admin sent back to a visitor (api/Contact/{id}/replies).</summary>
    public class ContactReplyDto
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}
