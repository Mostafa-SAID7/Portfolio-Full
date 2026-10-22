using System;

namespace PortfolioAPI.Core.DTOs
{
    /// <summary>Read model for a contact-form message (api/Contact inbox).</summary>
    public class ContactDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Payload accepted when submitting the public contact form.</summary>
    public class CreateContactDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

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

    /// <summary>Payload accepted by POST api/Contact/{id}/reply.</summary>
    public class ReplyToContactDto
    {
        public string Body { get; set; } = string.Empty;
    }
}
